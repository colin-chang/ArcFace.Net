using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core.Utils;
using ColinChang.ArcFace.Core.Extensions;

namespace ColinChang.ArcFace.Core;

/// <summary>
/// 人脸库管理 初始化/新增人脸/删除人脸/搜索人脸
/// </summary>
public partial class ArcFace
{
    /// <summary>
    /// 人脸库
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Face>> _faceLibraries =
        new() { ["default"] = new ConcurrentDictionary<string, Face>() };

    public async Task InitFaceLibraryAsync(IEnumerable<string> images, string libraryKey = "default")
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images.ToArray());
        if (exceptions.Any())
            throw new AggregateException(exceptions);

        await InitFaceLibraryAsync(faces, libraryKey);
    }

    public async Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<string> images,
        string libraryKey = "default") =>
        await TryInitFaceLibraryAsync((await ExtractFaceFeaturesAsync(images.ToArray())).Faces, libraryKey);

    public async Task InitFaceLibraryAsync(IEnumerable<Face> faces, string libraryKey = "default") =>
        await Task.Run(() => _faceLibraries.GetLibrary(libraryKey).InitFaceLibrary(faces));


    public async Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<Face> faces,
        string libraryKey = "default") =>
        await Task.FromResult(_faceLibraries.GetLibrary(libraryKey).TryInitFaceLibrary(faces));

    public async Task AddFaceAsync(string libraryKey = "default", params string[] images)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images);
        if (exceptions.Any())
            throw exceptions.FirstOrDefault();
        _faceLibraries.GetLibrary(libraryKey).AddFace(faces);
    }

    public async Task<(bool Success, int SuccessCount)> TryAddFaceAsync(string libraryKey = "default",
        params string[] images)
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(images);
        return _faceLibraries.GetLibrary(libraryKey).TryAddFace(faces);
    }

    public async Task AddFaceAsync(string libraryKey = "default", params Face[] faces) =>
        await Task.Run(() => _faceLibraries.GetLibrary(libraryKey).AddFace(faces));

    public async Task<(bool Success, int SuccessCount)>
        TryAddFaceAsync(string libraryKey = "default", params Face[] faces) =>
        await Task.Run(() => _faceLibraries.GetLibrary(libraryKey).TryAddFace(faces));

    public async Task<int> RemoveFaceAsync(string libraryKey = "default", params string[] faceIds) =>
        await Task.Run(() =>
        {
            var cnt = 0;
            if (faceIds == null || !faceIds.Any())
                return cnt;

            foreach (var faceId in faceIds)
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibraries.GetLibrary(libraryKey).ContainsKey(faceId))
                    continue;

                _faceLibraries.GetLibrary(libraryKey).Remove(faceId, out var face);
                face.Dispose();
                cnt++;
            }

            return cnt;
        });

    public async Task<(bool Success, int SuccessCount)> TryRemoveFaceAsync(string libraryKey = "default",
        params string[] faceIds) =>
        await Task.Run(() =>
        {
            var cnt = 0;
            if (faceIds == null || !faceIds.Any())
                return (true, cnt);

            foreach (var faceId in faceIds)
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibraries.GetLibrary(libraryKey).ContainsKey(faceId))
                    continue;

                var success = _faceLibraries.GetLibrary(libraryKey).TryRemove(faceId, out var face);
                if (!success)
                    continue;

                face.Dispose();
                cnt++;
            }

            return (cnt >= faceIds.Count(), cnt);
        });

    public async Task<OperationResult<Recognitions>>
        SearchFaceAsync(string image, Predicate<Face> predicate = null, string libraryKey = "default") =>
        await SearchFaceAsync(image, _options.MinSimilarity, predicate, libraryKey);

    public async Task<OperationResult<Recognitions>> SearchFaceAsync(string image, float minSimilarity,
        Predicate<Face> predicate = null, string libraryKey = "default")
    {
        if (!File.Exists(image))
            return new OperationResult<Recognitions>(null);

        await using var img = image.ToStream();
        return await SearchFaceAsync(img, minSimilarity, predicate, libraryKey);
    }

    public async Task<OperationResult<Recognitions>>
        SearchFaceAsync(Stream image, Predicate<Face> predicate = null, string libraryKey = "default") =>
        await SearchFaceAsync(image, _options.MinSimilarity, predicate, libraryKey);

    public async Task<OperationResult<Recognitions>> SearchFaceAsync(Stream image, float minSimilarity,
        Predicate<Face> predicate = null, string libraryKey = "default")
    {
        var (faces, exceptions) = await ExtractFaceFeaturesAsync(image);
        if (exceptions.Any())
            return new OperationResult<Recognitions>(exceptions.SingleOrDefault().Code);

        return await SearchFaceAsync(faces.SingleOrDefault().FeatureBytes, minSimilarity, predicate, libraryKey);
    }


    public async Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature,
        Predicate<Face> predicate = null, string libraryKey = "default") =>
        await SearchFaceAsync(feature, _options.MinSimilarity, predicate, libraryKey);


    public async Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature, float minSimilarity,
        Predicate<Face> predicate = null, string libraryKey = "default") =>
        await Task.Run(async () =>
        {
            var featureInfo = IntPtr.Zero;
            var library = predicate == null
                ? _faceLibraries.GetLibrary(libraryKey)
                : _faceLibraries.GetLibrary(libraryKey).Where(kv => predicate.Invoke(kv.Value));
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