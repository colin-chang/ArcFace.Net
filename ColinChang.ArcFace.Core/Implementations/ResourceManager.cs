using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core.Extensions;
using ColinChang.ArcFace.Core.Utils;

namespace ColinChang.ArcFace.Core;

/// <summary>
/// 资源管理 激活/引擎池管理/资源回收
/// </summary>
public partial class ArcFace
{
    #region 引擎池

    private readonly ConcurrentQueue<IntPtr> _imageEngines = new ConcurrentQueue<IntPtr>();
    private const int ASF_IMAGE_ENGINES_COUNT = 0;
    private readonly EventWaitHandle _imageWaitHandle = new AutoResetEvent(true);

    private readonly ConcurrentQueue<IntPtr> _videoEngines = new ConcurrentQueue<IntPtr>();
    private const int ASF_VIDEO_ENGINES_COUNT = 0;
    private readonly EventWaitHandle _videoWaitHandle = new AutoResetEvent(true);

    private readonly ConcurrentQueue<IntPtr> _rgbEngines = new ConcurrentQueue<IntPtr>();
    private const int ASF_RGB_ENGINES_COUNT = 0;
    private readonly EventWaitHandle _rgbWaitHandle = new AutoResetEvent(true);

    private readonly ConcurrentQueue<IntPtr> _irEngines = new ConcurrentQueue<IntPtr>();
    private const int ASF_IR_ENGINES_COUNT = 0;
    private readonly EventWaitHandle _irWaitHandle = new AutoResetEvent(true);

    #endregion

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
        //仅当引擎池已满时 回收后才需要开门，未满时在创建引擎后就立即开门
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
        foreach (var library in _faceLibraries.Values)
        {
            foreach (var face in library.Values)
                face.Feature.DisposeFaceFeature();
        }
    }
}