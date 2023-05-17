using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Extensions;
using ColinChang.ArcFace.Utils;

namespace ColinChang.ArcFace;

/// <summary>
/// 人脸库管理 初始化/新增人脸/删除人脸/搜索人脸
/// </summary>
public partial class ArcFace
{
    /// <summary>
    /// 人脸库
    /// </summary>
    private readonly ConcurrentDictionary<string, Face> _faceLibrary = new();

    public async Task InitFaceLibraryAsync(IEnumerable<string> images)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images.ToArray());
        if (exceptions.Any())
            throw new AggregateException(exceptions);

        await InitFaceLibraryAsync(faces);
    }

    public async Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<string> images) =>
        await TryInitFaceLibraryAsync((await ExtractFaceFeaturesAsync(images.ToArray())).Faces);

    public async Task InitFaceLibraryAsync(IEnumerable<Face> faces) =>
        await Task.Run(() => _faceLibrary.InitFaceLibrary(faces));


    public async Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<Face> faces) =>
        await Task.FromResult(_faceLibrary.TryInitFaceLibrary(faces));

    public async Task AddFaceAsync(params string[] images)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images);
        if (exceptions.Any())
            throw exceptions.FirstOrDefault();
        _faceLibrary.AddFace(faces);
    }

    public async Task<(bool Success, int SuccessCount)> TryAddFaceAsync(params string[] images)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images);
        return _faceLibrary.TryAddFace(faces);
    }

    public async Task AddFaceAsync(params Face[] faces) =>
        await Task.Run(() => _faceLibrary.AddFace(faces));

    public async Task<(bool Success, int SuccessCount)> TryAddFaceAsync(params Face[] faces) =>
        await Task.Run(() => _faceLibrary.TryAddFace(faces));

    public async Task<int> RemoveFaceAsync(params string[] faceIds) =>
        await Task.Run(() =>
        {
            var cnt = 0;
            if (faceIds == null || !faceIds.Any())
                return cnt;

            foreach (var faceId in faceIds)
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibrary.ContainsKey(faceId))
                    continue;

                _faceLibrary.Remove(faceId, out var face);
                face.Dispose();
                cnt++;
            }

            return cnt;
        });

    public async Task<(bool Success, int SuccessCount)> TryRemoveFaceAsync(params string[] faceIds) =>
        await Task.Run(() =>
        {
            var cnt = 0;
            if (faceIds == null || !faceIds.Any())
                return (true, cnt);

            foreach (var faceId in faceIds)
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibrary.ContainsKey(faceId))
                    continue;

                var success = _faceLibrary.TryRemove(faceId, out var face);
                if (!success)
                    continue;

                face.Dispose();
                cnt++;
            }

            return (cnt >= faceIds.Count(), cnt);
        });

    public async Task<OperationResult<Recognitions>>
        SearchFaceAsync(string image, Predicate<Face> predicate = null) =>
        await SearchFaceAsync(image, _options.MinSimilarity, predicate);

    public async Task<OperationResult<Recognitions>> SearchFaceAsync(string image, float minSimilarity,
        Predicate<Face> predicate = null)
    {
        if (!File.Exists(image))
            return new OperationResult<Recognitions>(null);

        await using var img = image.ToStream();
        return await SearchFaceAsync(img, minSimilarity, predicate);
    }

    public async Task<OperationResult<Recognitions>>
        SearchFaceAsync(Stream image, Predicate<Face> predicate = null) =>
        await SearchFaceAsync(image, _options.MinSimilarity, predicate);

    public async Task<OperationResult<Recognitions>> SearchFaceAsync(Stream image, float minSimilarity,
        Predicate<Face> predicate = null)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(image);
        if (exceptions.Any())
            return new OperationResult<Recognitions>(exceptions.SingleOrDefault().Code);

        return await SearchFaceAsync(faces.SingleOrDefault().FeatureBytes, minSimilarity, predicate);
    }


    public async Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature,
        Predicate<Face> predicate = null) =>
        await SearchFaceAsync(feature, _options.MinSimilarity, predicate);


    public async Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature, float minSimilarity,
        Predicate<Face> predicate = null) =>
        await Task.Run(async () =>
        {
            var featureInfo = IntPtr.Zero;
            var library = predicate == null
                ? _faceLibrary
                : _faceLibrary.Where(kv => predicate.Invoke(kv.Value));
            var recognitions = new ConcurrentBag<Recognition>();

            try
            {
                featureInfo = feature.ToFaceFeature();
                var groups = _options.MaxSingleTypeEngineCount;
                var stepSize = (int)Math.Ceiling(library.Count() * 1.0 / groups);
                var tasks = new Task[groups];

                for (var i = 0; i < groups; i++)
                {
                    var subLib = library.Skip(i * stepSize).Take(stepSize);
                    tasks[i] = SearchFaceAsync(featureInfo, subLib, minSimilarity, recognitions);
                }

                await Task.WhenAll(tasks);
                return new OperationResult<Recognitions>(new Recognitions(recognitions));
            }
            finally
            {
                featureInfo.DisposeFaceFeature();
            }
        });

    private Task SearchFaceAsync(IntPtr featureInfo, IEnumerable<KeyValuePair<string, Face>> library,
        float minSimilarity, ConcurrentBag<Recognition> recognitions) =>
        Task.Run(() =>
        {
            var engine = IntPtr.Zero;
            try
            {
                //当前版本使用同一个引擎句柄不支持多线程调用同一个算法接口，若需要对同一个接口进行多线程调用需要启动多个引擎。
                engine = GetEngine(DetectionModeEnum.Image);
                foreach (var (faceId, face) in library)
                {
                    var similarity = 0f;
                    var code = AsfHelper.ASFFaceFeatureCompare(engine, featureInfo, face.Feature,
                        ref similarity);
                    if (code != 0)
                        continue;
                    if (similarity <= minSimilarity)
                        continue;

                    recognitions.Add(new Recognition(faceId, similarity));
                }
            }
            finally
            {
                RecycleEngine(engine, DetectionModeEnum.Image);
            }
        });
}