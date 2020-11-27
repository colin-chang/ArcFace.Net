using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AForge.Imaging;
using ColinChang.FaceRecognition.Models;
using ColinChang.FaceRecognition.Utils;
using Microsoft.Extensions.Options;
using Image = System.Drawing.Image;

namespace ColinChang.FaceRecognition
{
    public class FaceRecognizer : IFaceRecognizer, IDisposable
    {
        private readonly FaceRecognitionOptions _options;

        #region 引擎控制

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

        #region 图像要求

        private readonly string[] _supportedImageExtensions = {".jpg", ".png", ".bmp", ".gif"};

        //图像尺寸上限
        private readonly long _maxImageSize = 10 * 1024 * 1024;

        private readonly int _minImageSize = 2 * 1024;

        //图像人脸角度上、下、左、右转向小于30度
        private readonly int _maxRotation = 30;

        //图片中人脸最小尺寸
        private readonly (int Width, int Height) _minFaceSize = (50, 50);

        #endregion

        public FaceRecognizer(IOptions<FaceRecognitionOptions> options) : this(options.Value)
        {
        }

        public FaceRecognizer(FaceRecognitionOptions options)
        {
            _options = options;
            OnlineActive();
        }

        /// <summary>
        /// 获取激活文件信息
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ActiveFileInfo> GetActiveFileInfoAsync()
        {
            return await Task.Run(() =>
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfActiveFileInfo>());
                var code = ASFFunctions.ASFGetActiveFileInfo(pointer);
                if (code != 0)
                {
                    Marshal.FreeHGlobal(pointer);
                    throw new Exception($"failed to get active file info. error code {code}");
                }

                var info = Marshal.PtrToStructure<AsfActiveFileInfo>(pointer);
                Marshal.FreeHGlobal(pointer);
                return info.Cast();
            });
        }

        /// <summary>
        /// 获取SDK版本信息
        /// </summary>
        /// <returns></returns>
        public async Task<VersionInfo> GetSdkVersionAsync()
        {
            return await Task.Run(() =>
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfVersionInfo>());
                ASFFunctions.ASFGetVersion(pointer);
                var version = Marshal.PtrToStructure<AsfVersionInfo>(pointer);
                Marshal.FreeHGlobal(pointer);
                return version.Cast();
            });
        }

        /// <summary>
        /// 人脸检测
        /// </summary>
        public async Task<MultiFaceInfo> DetectFaceAsync(string image)
        {
            VerifyImage(image);
            Image img = null;
            var engine = IntPtr.Zero;
            try
            {
                img = Image.FromFile(image);
                if (img == null)
                    return default;

                //缩放
                if (img.Width > 1536 || img.Height > 1536)
                    img = ImageUtil.ScaleImage(img, 1536, 1536);
                if (img.Width % 4 != 0)
                    img = ImageUtil.ScaleImage(img, img.Width - (img.Width % 4), img.Height);

                //人脸检测
                engine = GetEngine(DetectionModeEnum.IMAGE);
                var multiFaceInfo = await FaceUtil.DetectFaceAsync(engine, img);
                return multiFaceInfo.Cast();
            }
            finally
            {
                img?.Dispose();
                RecycleEngine(engine, DetectionModeEnum.IMAGE);
            }
        }

        /// <summary>
        /// 人脸特征提取
        /// </summary>
        /// <param name="picture"></param>
        /// <returns></returns>
        public async Task FaceFeatureExtractAsync(string image)
        {
            var engine = GetEngine(DetectionModeEnum.IMAGE);
            var img = Image.FromFile(image);
            var feature = await FaceUtil.ExtractFeatureAsync(engine, img);
        }

        /// <summary>
        /// 在线激活
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void OnlineActive()
        {
            var code = ASFFunctions.ASFOnlineActivation(_options.AppId, _options.SdkKey);
            if (code != 90114)
                throw new Exception($"failed to active. error code:{code}");
        }

        /// <summary>
        /// 从引擎池中获取引擎
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private IntPtr GetEngine(DetectionModeEnum mode)
        {
            IntPtr engine;
            var (engines, enginesCount, waitHandle) = GetEngineStuff(mode);

            //引擎池中有可用引擎则直接返回
            if (engines.TryDequeue(out engine))
                return engine;

            //无可用引擎时需要等待 
            waitHandle.WaitOne();
            if (enginesCount >= _options.MaxSingleTypeEngineCount)
                return GetEngine(mode);

            //引擎池未满则可以直接创建
            engine = InitEngine(mode);
            enginesCount++;
            if (enginesCount < _options.MaxSingleTypeEngineCount)
                waitHandle.Set();
            return engine;
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
                DetectionModeEnum.IMAGE => ASFFunctions.ASFInitEngine(
                    ASF_DetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
                    _options.ImageDetectFaceScaleVal, _options.DetectFaceMaxNum,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_AGE |
                    FaceEngineMask.ASF_GENDER | FaceEngineMask.ASF_FACE3DANGLE, ref engine),
                DetectionModeEnum.VIDEO => ASFFunctions.ASFInitEngine(
                    ASF_DetectionMode.ASF_DETECT_MODE_VIDEO, _options.VideoDetectFaceOrientPriority,
                    _options.VideoDetectFaceScaleVal, _options.DetectFaceMaxNum,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION, ref engine),
                DetectionModeEnum.RGB => ASFFunctions.ASFInitEngine(
                    ASF_DetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
                    _options.VideoDetectFaceScaleVal, 1,
                    FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_LIVENESS,
                    ref engine),
                DetectionModeEnum.IR => ASFFunctions.ASFInitEngine(
                    ASF_DetectionMode.ASF_DETECT_MODE_IMAGE, _options.ImageDetectFaceOrientPriority,
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
        private void UninitEngine(ConcurrentQueue<IntPtr> engines)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                while (!engines.IsEmpty)
                {
                    if (!engines.TryDequeue(out var engine))
                        Thread.Sleep(1000);

                    var code = ASFFunctions.ASFUninitEngine(engine);
                    if (code != 0)
                        engines.Enqueue(engine);
                }
            });
        }

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
        }

        #region 工具方法

        private (ConcurrentQueue<IntPtr> Engines, int EnginesCount, EventWaitHandle EnginesWaitHandle) GetEngineStuff(
            DetectionModeEnum mode) =>
            mode switch
            {
                DetectionModeEnum.IMAGE => (_imageEngines, _imageEnginesCount, _imageWaitHandle),
                DetectionModeEnum.VIDEO => (_videoEngines, _videoEnginesCount, _videoWaitHandle),
                DetectionModeEnum.RGB => (_rgbEngines, _rgbEnginesCount, _rgbWaitHandle),
                DetectionModeEnum.IR => (_irEngines, _irEnginesCount, _irWaitHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "invalid detection mode")
            };

        private void VerifyImage(string image)
        {
            if (!File.Exists(image))
                throw new FileNotFoundException();

            var size = new FileInfo(image).Length;
            if (size > _maxImageSize)
                throw new Exception($"image is oversize than {_maxImageSize}B.");
            if (size < _minImageSize)
                throw new Exception($"image is too small than {_minImageSize}B");

            if (!_supportedImageExtensions.Contains(Path.GetExtension(image).ToLower()))
                throw new UnsupportedImageFormatException("unsupported image type.");

            Image img = null;
            try
            {
                img = Image.FromFile(image);
            }
            catch
            {
                throw new UnsupportedImageFormatException("unsupported image type.");
            }
            finally
            {
                img?.Dispose();
            }
        }

        #endregion


        // public Task RegisterAsync(string faceLibrary)
        // {
        //     return Task.Run(() =>
        //     {
        //         _faceLibrary = faceLibrary;
        //
        //         LoadImages();
        //         foreach (var img in _images)
        //         {
        //             if (File.Exists($"{img}.ffd"))
        //                 continue;
        //
        //             DetectFace(img, true);
        //         }
        //
        //         LoadFfds();
        //     });
        // }
        //
        // public async Task<Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>> RecognizeFaceAsync(
        //     string image,
        //     float similarity)
        // {
        //     if (string.IsNullOrWhiteSpace(image) || !File.Exists(image))
        //         return null;
        //
        //     return await CompareFaceAsync(image, similarity);
        // }
        //
        // public async Task<Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>> RecognizeFaceAsync(
        //     Stream image,
        //     float similarity)
        // {
        //     if (image == null || image.Length <= 0)
        //         return null;
        //
        //     return await CompareFaceAsync(image, similarity);
        // }
        //
        // public async Task<Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>> RecognizeFaceAsync(Image image,
        //     float similarity)
        // {
        //     if (image == null)
        //         return null;
        //
        //     return await CompareFaceAsync(image, similarity);
        // }
        //
        // private Task<Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>> CompareFaceAsync(object image,
        //     float similarity)
        // {
        //     return Task.Run(() =>
        //     {
        //         //key: FaceFeature
        //         //value: [source.jpg_$2.ffd]=0.7 意为 image与source.jpg中第2张脸匹配度为0.7
        //         var dict = new Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>();
        //         var features = DetectFace(image);
        //         foreach (var feature in features)
        //         {
        //             var res = Compare(feature.Content).Where(kv => kv.Value >= similarity);
        //             if (res.Any())
        //                 dict[feature] = res;
        //         }
        //
        //         return dict;
        //     });
        // }
        //
        //
        // public void Dispose()
        // {
        //     AFDFunction.AFD_FSDK_UninitialFaceEngine(_detectEngine);
        //     AFRFunction.AFR_FSDK_UninitialEngine(_recognizeEngine);
        // }
        //
        // private void LoadImages()
        // {
        //     _images.Clear();
        //     var formats = new[] {".jpg", ".jpeg", ".png", ".bmp"};
        //     var images = Directory.GetFiles(_faceLibrary, "*.*", SearchOption.AllDirectories);
        //     foreach (var img in images)
        //     {
        //         if (formats.Contains(Path.GetExtension(img)))
        //             _images.Add(img);
        //     }
        // }
        //
        // private void LoadFfds()
        // {
        //     _ffds.Clear();
        //     var ffds = Directory.GetFiles(_faceLibrary, "*.ffd", SearchOption.AllDirectories);
        //     foreach (var ffd in ffds)
        //     {
        //         var img = Path.GetFileNameWithoutExtension(ffd);
        //         img = img?.Substring(0, img.IndexOf("_$", StringComparison.Ordinal));
        //
        //         if (!File.Exists(Path.Combine(Path.GetDirectoryName(ffd), img)))
        //             continue;
        //
        //         _ffds.Add(ffd);
        //     }
        // }
        //
        // private IEnumerable<Feature> DetectFace(object source, bool register = false)
        // {
        //     byte[] imageData;
        //     int width, height, pitch;
        //
        //     if (source is Stream stream)
        //     {
        //         using (var img = Image.FromStream(stream))
        //             imageData = ReadImage(img, out width, out height, out pitch);
        //     }
        //     else if (source is Image img)
        //     {
        //         imageData = ReadImage(img, out width, out height, out pitch);
        //     }
        //     else
        //     {
        //         using (var img0 = Image.FromFile(source.ToString()))
        //             imageData = ReadImage(img0, out width, out height, out pitch);
        //     }
        //
        //     var imageDataPtr = Marshal.AllocHGlobal(imageData.Length);
        //     Marshal.Copy(imageData, 0, imageDataPtr, imageData.Length);
        //
        //     var offInput = new ASVLOFFSCREEN {u32PixelArrayFormat = 513, ppu8Plane = new IntPtr[4]};
        //     offInput.ppu8Plane[0] = imageDataPtr;
        //     offInput.i32Width = width;
        //     offInput.i32Height = height;
        //     offInput.pi32Pitch = new int[4];
        //     offInput.pi32Pitch[0] = pitch;
        //
        //     var faceRes = new AFD_FSDK_FACERES();
        //     var offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
        //     Marshal.StructureToPtr(offInput, offInputPtr, false);
        //     var faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceRes));
        //     AFDFunction.AFD_FSDK_StillImageFaceDetection(_detectEngine, offInputPtr, ref faceResPtr);
        //
        //     var obj = Marshal.PtrToStructure(faceResPtr, typeof(AFD_FSDK_FACERES));
        //     faceRes = (AFD_FSDK_FACERES) obj;
        //     var features = new List<Feature>();
        //     if (faceRes.nFace > 0)
        //     {
        //         var faceInput = new AFR_FSDK_FaceInput
        //         {
        //             lOrient = (int) Marshal.PtrToStructure(faceRes.lfaceOrient, typeof(int))
        //         };
        //         for (var i = 0; i < faceRes.nFace; i++)
        //         {
        //             var rect = (MRECT) Marshal.PtrToStructure(faceRes.rcFace + Marshal.SizeOf(typeof(MRECT)) * i,
        //                 typeof(MRECT));
        //             faceInput.rcFace = rect;
        //
        //             var faceInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceInput));
        //             Marshal.StructureToPtr(faceInput, faceInputPtr, false);
        //             var faceModel = new AFR_FSDK_FaceModel();
        //             var faceModelPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceModel));
        //             AFRFunction.AFR_FSDK_ExtractFRFeature(_recognizeEngine, offInputPtr, faceInputPtr,
        //                 faceModelPtr);
        //             faceModel = (AFR_FSDK_FaceModel) Marshal.PtrToStructure(faceModelPtr, typeof(AFR_FSDK_FaceModel));
        //
        //             var featureContent = new byte[faceModel.lFeatureSize];
        //             if (featureContent.Length > 0)
        //                 Marshal.Copy(faceModel.pbFeature, featureContent, 0, faceModel.lFeatureSize);
        //             features.Add(new Feature(register ? source.ToString() : null, i, rect.left, rect.top,
        //                 rect.right - rect.left,
        //                 rect.bottom - rect.top, featureContent));
        //
        //             if (register)
        //             {
        //                 var ffd = Path.Combine(_faceLibrary, $"{Path.GetFileName(source.ToString())}_${i}.ffd");
        //                 if (File.Exists(ffd))
        //                     File.Delete(ffd);
        //                 File.WriteAllBytes(ffd, featureContent);
        //             }
        //
        //             Marshal.FreeHGlobal(faceModelPtr);
        //             Marshal.FreeHGlobal(faceInputPtr);
        //         }
        //     }
        //
        //     Marshal.FreeHGlobal(offInputPtr);
        //     Marshal.FreeHGlobal(imageDataPtr);
        //
        //     return features;
        // }
        //
        //
        // private static byte[] ReadImage(Image image, out int width, out int height, out int pitch)
        // {
        //     var bitmap = new Bitmap(image);
        //     //将Bitmap锁定到系统内存中,获得BitmapData
        //     var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
        //         PixelFormat.Format24bppRgb);
        //     //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
        //     var ptr = data.Scan0;
        //     //定义数组长度
        //     var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
        //     var sourceBitArray = new byte[sourceBitArrayLength];
        //     //将bitmap中的内容拷贝到ptr_bgr数组中
        //     Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);
        //
        //     width = data.Width;
        //     height = data.Height;
        //
        //     pitch = Math.Abs(data.Stride);
        //
        //     var line = width * 3;
        //     var bgrLen = line * height;
        //     var destBitArray = new byte[bgrLen];
        //
        //     for (var i = 0; i < height; ++i)
        //         Array.Copy(sourceBitArray, i * pitch, destBitArray, i * line, line);
        //
        //     pitch = line;
        //     bitmap.UnlockBits(data);
        //     return destBitArray;
        // }
        //
        // private Dictionary<string, float> Compare(byte[] feature)
        // {
        //     var localFaceModels = new AFR_FSDK_FaceModel();
        //     var sourceFeaturePtr = Marshal.AllocHGlobal(feature.Length);
        //     Marshal.Copy(feature, 0, sourceFeaturePtr, feature.Length);
        //     localFaceModels.lFeatureSize = feature.Length;
        //     localFaceModels.pbFeature = sourceFeaturePtr;
        //     var firstPtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels));
        //     Marshal.StructureToPtr(localFaceModels, firstPtr, false);
        //
        //     var dict = new Dictionary<string, float>();
        //     foreach (var ffd in _ffds)
        //     {
        //         if (!File.Exists(ffd))
        //             continue;
        //
        //         var libraryFeature = File.ReadAllBytes(ffd);
        //         var localFaceModels2 = new AFR_FSDK_FaceModel();
        //         var libraryFeaturePtr = Marshal.AllocHGlobal(libraryFeature.Length);
        //         Marshal.Copy(libraryFeature, 0, libraryFeaturePtr, libraryFeature.Length);
        //         localFaceModels2.lFeatureSize = libraryFeature.Length;
        //         localFaceModels2.pbFeature = libraryFeaturePtr;
        //         var secondPtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels2));
        //         Marshal.StructureToPtr(localFaceModels2, secondPtr, false);
        //         var result = 0f;
        //         AFRFunction.AFR_FSDK_FacePairMatching(_recognizeEngine, firstPtr, secondPtr, ref result);
        //         dict[ffd] = result;
        //
        //         Marshal.FreeHGlobal(libraryFeaturePtr);
        //         Marshal.FreeHGlobal(secondPtr);
        //     }
        //
        //     Marshal.FreeHGlobal(sourceFeaturePtr);
        //     Marshal.FreeHGlobal(firstPtr);
        //     return dict;
        // }
    }
}