using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ColinChang.ArcFace.Models;
using ColinChang.ArcFace.Utils;
using Microsoft.Extensions.Options;
using Polly;

namespace ColinChang.ArcFace
{
    public class ArcFace : IArcFace
    {
        #region 图像质量要求

        /// <summary>
        /// 图像尺寸上限
        /// </summary>
        private const long ASF_MAX_IMAGE_SIZE = 10 * 1024 * 1024;

        /// <summary>
        /// 图像尺寸下线
        /// </summary>
        private const int ASF_MIN_IMAGE_SIZE = 2 * 1024;

        /// <summary>
        /// 支持的图片格式
        /// </summary>
        private readonly string[] _supportedImageExtensions = { ".jpeg", ".jpg", ".png", ".bmp" };

        #endregion

        #region 引擎池

        private readonly ConcurrentQueue<IntPtr> _imageEngines = new ConcurrentQueue<IntPtr>();
        private readonly int _imageEnginesCount = 0;
        private readonly EventWaitHandle _imageWaitHandle = new AutoResetEvent(true);

        private readonly ConcurrentQueue<IntPtr> _videoEngines = new ConcurrentQueue<IntPtr>();
        private readonly int _videoEnginesCount = 0;
        private readonly EventWaitHandle _videoWaitHandle = new AutoResetEvent(true);

        private readonly ConcurrentQueue<IntPtr> _rgbEngines = new ConcurrentQueue<IntPtr>();
        private readonly int _rgbEnginesCount = 0;
        private readonly EventWaitHandle _rgbWaitHandle = new AutoResetEvent(true);

        private readonly ConcurrentQueue<IntPtr> _irEngines = new ConcurrentQueue<IntPtr>();
        private readonly int _irEnginesCount = 0;
        private readonly EventWaitHandle _irWaitHandle = new AutoResetEvent(true);

        #endregion

        /// <summary>
        /// 人脸库
        /// </summary>
        // private readonly ConcurrentDictionary<string, IntPtr> _faceLibrary = new ConcurrentDictionary<string, IntPtr>();
        private readonly ConcurrentDictionary<string, Face> _faceLibrary = new ConcurrentDictionary<string, Face>();

        private readonly ArcFaceOptions _options;

        public ArcFace(IOptionsMonitor<ArcFaceOptions> options) : this(options.CurrentValue)
        {
        }

        public ArcFace(ArcFaceOptions options)
        {
            _options = options;
            OnlineActiveAsync().Wait();
        }

        #region SDK信息 激活信息/版本信息

        public async Task<OperationResult<ActiveFileInfo>> GetActiveFileInfoAsync() =>
            await Task.Run(() =>
            {
                var pointer = IntPtr.Zero;
                try
                {
                    pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfActiveFileInfo>());
                    var code = AsfHelper.ASFGetActiveFileInfo(pointer);
                    if (code != 0)
                        return new OperationResult<ActiveFileInfo>(code);

                    var info = Marshal.PtrToStructure<AsfActiveFileInfo>(pointer);
                    return new OperationResult<ActiveFileInfo>(info.Cast());
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                        Marshal.FreeHGlobal(pointer);
                }
            });

        public async Task<VersionInfo> GetSdkVersionAsync() =>
            await Task.Run(() =>
            {
                var pointer = IntPtr.Zero;
                try
                {
                    pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfVersionInfo>());
                    AsfHelper.ASFGetVersion(pointer);
                    var version = Marshal.PtrToStructure<AsfVersionInfo>(pointer);
                    return version.Cast();
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                }
            });

        #endregion

        #region 人脸属性 3D角度/年龄/性别

        public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(string image) =>
            await ProcessImageAsync<AsfFace3DAngle, Face3DAngle>(image, FaceHelper.GetFace3DAngleAsync);

        public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(Image image) =>
            await ProcessImageAsync<AsfFace3DAngle, Face3DAngle>(image, FaceHelper.GetFace3DAngleAsync);

        public async Task<OperationResult<AgeInfo>> GetAgeAsync(string image) =>
            await ProcessImageAsync<AsfAgeInfo, AgeInfo>(image, FaceHelper.GetAgeAsync);

        public async Task<OperationResult<AgeInfo>> GetAgeAsync(Image image) =>
            await ProcessImageAsync<AsfAgeInfo, AgeInfo>(image, FaceHelper.GetAgeAsync);

        public async Task<OperationResult<GenderInfo>> GetGenderAsync(string image) =>
            await ProcessImageAsync<AsfGenderInfo, GenderInfo>(image, FaceHelper.GetGenderAsync);

        public async Task<OperationResult<GenderInfo>> GetGenderAsync(Image image) =>
            await ProcessImageAsync<AsfGenderInfo, GenderInfo>(image, FaceHelper.GetGenderAsync);

        #endregion

        #region 核心功能 人脸检测/特征提取/人脸比对

        public async Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(string image) =>
            await ProcessImageAsync<AsfMultiFaceInfo, MultiFaceInfo>(image, FaceHelper.DetectFaceAsync);

        public async Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(Image image) =>
            await ProcessImageAsync<AsfMultiFaceInfo, MultiFaceInfo>(image, FaceHelper.DetectFaceAsync);

        public async Task<OperationResult<MultiFaceInfo>> DetectFaceFromBase64StringAsync(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                return new OperationResult<MultiFaceInfo>(default);

            using var image = Image.FromStream(new MemoryStream(Convert.FromBase64String(base64Image)));
            return await ProcessImageAsync<AsfMultiFaceInfo, MultiFaceInfo>(image, FaceHelper.DetectFaceAsync);
        }

        public async Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(Image image, LivenessMode mode)
        {
            if (mode == LivenessMode.RGB)
                return await ProcessImageAsync<AsfLivenessInfo, LivenessInfo>(image, FaceHelper.GetRgbLivenessInfoAsync,
                    DetectionModeEnum.RGB);

            return await ProcessImageAsync<AsfLivenessInfo, LivenessInfo>(image, FaceHelper.GetIrLivenessInfoAsync,
                DetectionModeEnum.IR);
        }

        public async Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(string image) =>
            await ProcessImageAsync<IEnumerable<byte[]>, IEnumerable<byte[]>>(image,
                FaceHelper.ExtractFeatureAsync);

        public async Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(Image image) =>
            await ProcessImageAsync<IEnumerable<byte[]>, IEnumerable<byte[]>>(image,
                FaceHelper.ExtractFeatureAsync);

        public async Task<OperationResult<float>> CompareFaceFeatureAsync(byte[] feature1, byte[] feature2) =>
            await Task.Run(() =>
            {
                var engine = IntPtr.Zero;
                var featureA = IntPtr.Zero;
                var featureB = IntPtr.Zero;
                try
                {
                    engine = GetEngine(DetectionModeEnum.Image);
                    featureA = feature1.ToFaceFeature();
                    featureB = feature2.ToFaceFeature();

                    var similarity = 0f;
                    var code = AsfHelper.ASFFaceFeatureCompare(engine, featureA, featureB, ref similarity);
                    return new OperationResult<float>(similarity, code);
                }
                finally
                {
                    RecycleEngine(engine, DetectionModeEnum.Image);
                    featureA.DisposeFaceFeature();
                    featureB.DisposeFaceFeature();
                }
            });

        #endregion

        #region 人脸库管理 初始化/新增人脸/删除人脸/搜索人脸

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
            await Task.Run(() => (_faceLibrary.TryInitFaceLibrary(faces), _faceLibrary.Count));

        public async Task AddFaceAsync(string image)
        {
            var (faces, exceptions) = await ExtractFaceFeaturesAsync(image);
            if (exceptions.Any())
                throw exceptions.FirstOrDefault();
            _faceLibrary.AddFace(faces.FirstOrDefault());
        }

        public async Task<bool> TryAddFaceAsync(string image)
        {
            var (faces, exceptions) = await ExtractFaceFeaturesAsync(image);
            return !exceptions.Any() && _faceLibrary.TryAddFace(faces.FirstOrDefault());
        }

        public async Task AddFaceAsync(Face face) =>
            await Task.Run(() => _faceLibrary.AddFace(face));

        public async Task<bool> TryAddFaceAsync(Face face) =>
            await Task.Run(() => _faceLibrary.TryAddFace(face));

        public async Task RemoveFaceAsync(string faceId) =>
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibrary.ContainsKey(faceId))
                    return;

                _faceLibrary.Remove(faceId, out var face);
                face.Dispose();
            });

        public async Task<bool> TryRemoveFaceAsync(string faceId) =>
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(faceId) || !_faceLibrary.ContainsKey(faceId))
                    return false;

                var success = _faceLibrary.TryRemove(faceId, out var face);
                if (success)
                    face.Dispose();
                return success;
            });

        public async Task<OperationResult<Recognition>>
            SearchFaceAsync(string image, Predicate<Face> predicate = null) =>
            await SearchFaceAsync(image, _options.MinSimilarity, predicate);

        public async Task<OperationResult<Recognition>> SearchFaceAsync(string image, float minSimilarity,
            Predicate<Face> predicate = null)
        {
            if (!File.Exists(image))
                return new OperationResult<Recognition>(null);

            using var img = Image.FromFile(image);
            return await SearchFaceAsync(img, minSimilarity, predicate);
        }

        public async Task<OperationResult<Recognition>>
            SearchFaceAsync(Image image, Predicate<Face> predicate = null) =>
            await SearchFaceAsync(image, _options.MinSimilarity, predicate);

        public async Task<OperationResult<Recognition>> SearchFaceAsync(Image image, float minSimilarity,
            Predicate<Face> predicate = null)
        {
            var (faces, exceptions) = await ExtractFaceFeaturesAsync(image);
            if (exceptions.Any())
                return new OperationResult<Recognition>(exceptions.SingleOrDefault().Code);

            return await SearchFaceAsync(faces.SingleOrDefault().FeatureBytes, minSimilarity, predicate);
        }

        public async Task<OperationResult<Recognition>> SearchFaceAsync(byte[] feature,
            Predicate<Face> predicate = null) =>
            await SearchFaceAsync(feature, _options.MinSimilarity, predicate);


        public async Task<OperationResult<Recognition>> SearchFaceAsync(byte[] feature, float minSimilarity,
            Predicate<Face> predicate = null) =>
            await Task.Run(() =>
            {
                var engine = IntPtr.Zero;
                var featureInfo = IntPtr.Zero;
                try
                {
                    featureInfo = feature.ToFaceFeature();
                    var recognition = new Recognition();
                    engine = GetEngine(DetectionModeEnum.Image);
                    var library = predicate == null
                        ? _faceLibrary
                        : _faceLibrary.Where(kv => predicate.Invoke(kv.Value));
                    foreach (var (faceId, face) in library)
                    {
                        var similarity = 0f;
                        var code = AsfHelper.ASFFaceFeatureCompare(engine, featureInfo, face.Feature, ref similarity);
                        if (code != 0)
                            continue;
                        if (similarity <= recognition.Similarity)
                            continue;

                        recognition.Similarity = similarity;
                        recognition.FaceId = faceId;
                    }

                    recognition = recognition.Similarity < minSimilarity ? null : recognition;
                    return new OperationResult<Recognition>(recognition);
                }
                finally
                {
                    featureInfo.DisposeFaceFeature();
                    RecycleEngine(engine, DetectionModeEnum.Image);
                }
            });

        #endregion

        #region 资源管理 激活/引擎池管理/资源回收

        /// <summary>
        /// 在线激活
        /// </summary>
        /// <exception cref="Exception"></exception>
        private async Task OnlineActiveAsync()
        {
            string sdkKey;
            var platform = Environment.OSVersion.Platform;
            var is64Bit = Environment.Is64BitProcess;
            if (platform == PlatformID.Win32NT)
                sdkKey = is64Bit ? _options.SdkKeys.Winx64 : _options.SdkKeys.Winx86;
            else if (platform == PlatformID.Unix && is64Bit)
                sdkKey = _options.SdkKeys.Linux64;
            else
                throw new NotSupportedException("only Windows(x86/x64) and Linux(x64) are supported");

            await Policy.Handle<Exception>()
                .RetryAsync(4)
                .ExecuteAsync(async () =>
                {
                    var code = AsfHelper.ASFOnlineActivation(_options.AppId, sdkKey);
                    if (code != 90114)
                        throw new Exception($"failed to active. error code:{code}. please try again");
                    await Task.CompletedTask;
                });
        }

        /// <summary>
        /// 从引擎池中获取引擎
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private IntPtr GetEngine(DetectionModeEnum mode)
        {
            while (true)
            {
                var (engines, enginesCount, waitHandle) = GetEngineStuff(mode);

                //引擎池中有可用引擎则直接返回
                if (engines.TryDequeue(out var engine)) return engine;

                //无可用引擎时需要等待 
                waitHandle.WaitOne();
                if (enginesCount >= _options.MaxSingleTypeEngineCount) continue;

                //引擎池未满则可以直接创建
                engine = InitEngine(mode);
                enginesCount++;
                if (enginesCount < _options.MaxSingleTypeEngineCount) waitHandle.Set();
                return engine;
            }
        }

        /// <summary>
        /// 初始化引擎
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IntPtr InitEngine(DetectionModeEnum mode)
        {
            var engine = IntPtr.Zero;
            var code = mode switch
            {
                DetectionModeEnum.Image => AsfHelper.ASFInitEngine(
                    AsfDetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
                    _options.ImageDetectFaceScaleVal, _options.MaxDetectFaceNum,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_AGE |
                    FaceEngineMask.ASF_GENDER | FaceEngineMask.ASF_FACE3DANGLE, ref engine),
                DetectionModeEnum.Video => AsfHelper.ASFInitEngine(
                    AsfDetectionMode.ASF_DETECT_MODE_VIDEO, _options.VideoDetectFaceOrientPriority,
                    _options.VideoDetectFaceScaleVal, _options.MaxDetectFaceNum,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION, ref engine),
                DetectionModeEnum.RGB => AsfHelper.ASFInitEngine(
                    AsfDetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
                    _options.VideoDetectFaceScaleVal, 1,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_LIVENESS,
                    ref engine),
                DetectionModeEnum.IR => AsfHelper.ASFInitEngine(
                    AsfDetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
                    _options.VideoDetectFaceScaleVal, 1,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION |
                    FaceEngineMask.ASF_IR_LIVENESS, ref engine),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            if (code != 0)
                throw new Exception($"failed to init engine. error code {code}");
            return engine;
        }

        /// <summary>
        /// 回收引擎
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="mode"></param>
        private void RecycleEngine(IntPtr engine, DetectionModeEnum mode)
        {
            if (engine == IntPtr.Zero)
                return;

            var (engines, enginesCount, waitHandle) = GetEngineStuff(mode);
            engines.Enqueue(engine);
            //尽当引擎池已满时 回收后才需要开门，未满时在创建引擎后就立即开门
            if (enginesCount >= _options.MaxSingleTypeEngineCount)
                waitHandle.Set();
        }

        /// <summary>
        /// 销毁引擎
        /// </summary>
        /// <param name="engines"></param>
        private void UninitEngine(ConcurrentQueue<IntPtr> engines) =>
            ThreadPool.QueueUserWorkItem(state =>
            {
                while (!engines.IsEmpty)
                {
                    if (!engines.TryDequeue(out var engine))
                        Thread.Sleep(1000);

                    var code = AsfHelper.ASFUninitEngine(engine);
                    if (code != 0)
                        engines.Enqueue(engine);
                }
            });

        public void Dispose()
        {
            //释放 WaitHandle
            _imageWaitHandle.Close();
            _imageWaitHandle.Dispose();
            _videoWaitHandle.Close();
            _videoWaitHandle.Dispose();

            //销毁所有引擎
            UninitEngine(_imageEngines);
            UninitEngine(_videoEngines);
            UninitEngine(_rgbEngines);
            UninitEngine(_irEngines);

            //释放 人脸库资源
            foreach (var face in _faceLibrary.Values)
                face.Feature.DisposeFaceFeature();
        }

        #endregion

        #region 工具方法

        private (ConcurrentQueue<IntPtr> Engines, int EnginesCount, EventWaitHandle EnginesWaitHandle) GetEngineStuff(
            DetectionModeEnum mode) =>
            mode switch
            {
                DetectionModeEnum.Image => (_imageEngines, _imageEnginesCount, _imageWaitHandle),
                DetectionModeEnum.Video => (_videoEngines, _videoEnginesCount, _videoWaitHandle),
                DetectionModeEnum.RGB => (_rgbEngines, _rgbEnginesCount, _rgbWaitHandle),
                DetectionModeEnum.IR => (_irEngines, _irEnginesCount, _irWaitHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "invalid detection mode")
            };

        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(string image,
            Func<IntPtr, Image, Task<OperationResult<T>>> process, DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            if (!File.Exists(image))
                return new OperationResult<TK>(default);

            using var img = Image.FromFile(image);
            return await ProcessImageAsync<T, TK>(img, process, mode);
        }

        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(Image image,
            Func<IntPtr, Image, Task<OperationResult<T>>> process, DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            var engine = IntPtr.Zero;
            try
            {
                image = VerifyImage(image);
                engine = GetEngine(mode);
                return (await process(engine, image)).Cast<TK>();
            }
            finally
            {
                RecycleEngine(engine, mode);
            }
        }

        private Image VerifyImage(string image)
        {
            if (!File.Exists(image))
                throw new FileNotFoundException($"{image} doesn't exist.");

            using var img = Image.FromFile(image);
            return VerifyImage(img);
        }

        private Image VerifyImage(Image image)
        {
            if (image == null)
                throw new FileNotFoundException("image cannot be null.");

            using var stream = new MemoryStream();
            image.Save(stream, image.RawFormat);
            var length = stream.Length;
            if (length > ASF_MAX_IMAGE_SIZE)
                throw new Exception($"image is oversize than {ASF_MAX_IMAGE_SIZE}B.");
            if (length < ASF_MIN_IMAGE_SIZE)
                throw new Exception($"image is too small than {ASF_MIN_IMAGE_SIZE}B");

            if (!_supportedImageExtensions.Contains($".{image.RawFormat.ToString().ToLower()}"))
                throw new Exception("unsupported image type.");

            return ScaleImage(image);
        }

        private static Image ScaleImage(Image image)
        {
            try
            {
                try
                {
                    //缩放
                    if (image.Width > 1536 || image.Height > 1536)
                        image = ImageHelper.ScaleImage(image, 1536, 1536);
                    if (image.Width % 4 != 0)
                        image = ImageHelper.ScaleImage(image, image.Width - image.Width % 4, image.Height);

                    return ImageHelper.ScaleImage(image, image.Width, image.Height);
                }
                catch
                {
                    throw new Exception("unsupported image type.");
                }
            }
            catch
            {
                throw new Exception("unsupported image type.");
            }
        }

        private async Task<(IEnumerable<Face> Faces, IEnumerable<NoFaceImageException> Exceptions)>
            ExtractFaceFeaturesAsync<T>(
                params T[] images)
        {
            var faces = new List<Face>();
            var exceptions = new List<NoFaceImageException>();
            if (images == null || !images.Any())
                return (faces, exceptions);

            var engine = IntPtr.Zero;
            try
            {
                engine = GetEngine(DetectionModeEnum.Image);
                foreach (var image in images)
                {
                    if (image is not Image && image is not string)
                        throw new Exception("invalid images type");
                    if (image is string image0)
                    {
                        using var img = VerifyImage(image0);
                        var feature = await FaceHelper.ExtractSingleFeatureAsync(engine, img);
                        if (feature.Code != 0)
                        {
                            exceptions.Add(
                                new NoFaceImageException(feature.Code, image0));
                            continue;
                        }

                        faces.Add(new Face(
                            Path.GetFileNameWithoutExtension(image0), feature.Data));
                    }
                    else
                    {
                        var img = VerifyImage(image as Image);
                        var feature = await FaceHelper.ExtractSingleFeatureAsync(engine, img);
                        if (feature.Code != 0)
                        {
                            exceptions.Add(
                                new NoFaceImageException(feature.Code));
                            continue;
                        }

                        faces.Add(new Face(null, feature.Data));
                    }
                }

                return (faces, exceptions);
            }
            finally
            {
                RecycleEngine(engine, DetectionModeEnum.Image);
            }
        }

        #endregion
    }
}