using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ColinChang.FaceRecognition.AFD;
using ColinChang.FaceRecognition.AFR;

namespace ColinChang.FaceRecognition
{
    public class Recognizer : IDisposable
    {
        private readonly IntPtr _detectEngine = IntPtr.Zero;
        private readonly IntPtr _recognizeEngine = IntPtr.Zero;
        private readonly List<string> _images = new List<string>();
        private readonly List<string> _ffds = new List<string>();
        private string _faceLibrary;

        public Recognizer(string appId, string fdKey, string frKey)
        {
            const int detectSize = 100 * 1024 * 1024;
            const int nScale = 16;
            const int nMaxFaceNum = 8;
            var pMem = Marshal.AllocHGlobal(detectSize);
            var pMemRecognize = Marshal.AllocHGlobal(detectSize);

            var fdCode = AFDFunction.AFD_FSDK_InitialFaceEngine(appId, fdKey, pMem, detectSize, ref _detectEngine, 5,
                nScale, nMaxFaceNum);
            if (fdCode != 0)
                throw new ArgumentException($"Initialize FaceDetectEngine failed.Error code:{fdCode}");

            var frCode =
                AFRFunction.AFR_FSDK_InitialEngine(appId, frKey, pMemRecognize, detectSize, ref _recognizeEngine);
            if (frCode != 0)
                throw new ArgumentException($"Initialize FaceRecognizeEngine failed.Error code:{fdCode}");
        }

        public void Register(string faceLibrary)
        {
            _faceLibrary = faceLibrary;

            LoadImages();
            foreach (var img in _images)
            {
                if (File.Exists($"{img}.ffd"))
                    continue;

                DetectFace(img, true);
            }

            LoadFfds();
        }

        public Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>> Compare(string image, float similarity)
        {
            if (string.IsNullOrWhiteSpace(image) || !File.Exists(image))
                return null;

            //key: FaceFeature
            //value: [source.jpg_$2.ffd]=0.7 意为 image与source.jpg中第2张脸匹配度为0.7
            var dict = new Dictionary<Feature, IEnumerable<KeyValuePair<string, float>>>();
            var features = DetectFace(image);
            foreach (var feature in features)
            {
                var res = Compare(feature.Content).Where(kv => kv.Value >= similarity);
                if (res.Any())
                    dict[feature] = res;
            }

            return dict;
        }

        public void Dispose()
        {
            AFDFunction.AFD_FSDK_UninitialFaceEngine(_detectEngine);
            AFRFunction.AFR_FSDK_UninitialEngine(_recognizeEngine);
        }

        private void LoadImages()
        {
            _images.Clear();
            var formats = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
            var images = Directory.GetFiles(_faceLibrary, "*.*", SearchOption.AllDirectories);
            foreach (var img in images)
            {
                if (formats.Contains(Path.GetExtension(img)))
                    _images.Add(img);
            }
        }

        private void LoadFfds()
        {
            _ffds.Clear();
            var ffds = Directory.GetFiles(_faceLibrary, "*.ffd", SearchOption.AllDirectories);
            foreach (var ffd in ffds)
            {
                var img = Path.GetFileNameWithoutExtension(ffd);
                img = img?.Substring(0, img.IndexOf("_$", StringComparison.Ordinal));

                if (!File.Exists(Path.Combine(Path.GetDirectoryName(ffd), img)))
                    continue;

                _ffds.Add(ffd);
            }
        }

        private IEnumerable<Feature> DetectFace(string source, bool register = false)
        {
            byte[] imageData;
            int width, height, pitch;
            using (var img = Image.FromFile(source))
                imageData = ReadImage(img, out width, out height, out pitch);

            var imageDataPtr = Marshal.AllocHGlobal(imageData.Length);
            Marshal.Copy(imageData, 0, imageDataPtr, imageData.Length);

            var offInput = new ASVLOFFSCREEN { u32PixelArrayFormat = 513, ppu8Plane = new IntPtr[4] };
            offInput.ppu8Plane[0] = imageDataPtr;
            offInput.i32Width = width;
            offInput.i32Height = height;
            offInput.pi32Pitch = new int[4];
            offInput.pi32Pitch[0] = pitch;

            var faceRes = new AFD_FSDK_FACERES();
            var offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
            Marshal.StructureToPtr(offInput, offInputPtr, false);
            var faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceRes));
            AFDFunction.AFD_FSDK_StillImageFaceDetection(_detectEngine, offInputPtr, ref faceResPtr);

            var obj = Marshal.PtrToStructure(faceResPtr, typeof(AFD_FSDK_FACERES));
            faceRes = (AFD_FSDK_FACERES)obj;
            var features = new List<Feature>();
            if (faceRes.nFace > 0)
            {
                var faceInput = new AFR_FSDK_FaceInput
                {
                    lOrient = (int)Marshal.PtrToStructure(faceRes.lfaceOrient, typeof(int))
                };
                for (var i = 0; i < faceRes.nFace; i++)
                {
                    var rect = (MRECT)Marshal.PtrToStructure(faceRes.rcFace + Marshal.SizeOf(typeof(MRECT)) * i,
                        typeof(MRECT));
                    faceInput.rcFace = rect;

                    var faceInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceInput));
                    Marshal.StructureToPtr(faceInput, faceInputPtr, false);
                    var faceModel = new AFR_FSDK_FaceModel();
                    var faceModelPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceModel));
                    AFRFunction.AFR_FSDK_ExtractFRFeature(_recognizeEngine, offInputPtr, faceInputPtr,
                        faceModelPtr);
                    faceModel = (AFR_FSDK_FaceModel)Marshal.PtrToStructure(faceModelPtr, typeof(AFR_FSDK_FaceModel));

                    var featureContent = new byte[faceModel.lFeatureSize];
                    if (featureContent.Length > 0)
                        Marshal.Copy(faceModel.pbFeature, featureContent, 0, faceModel.lFeatureSize);
                    features.Add(new Feature(source, i, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, featureContent));
                    if (register)
                    {
                        var ffd = Path.Combine(_faceLibrary, $"{Path.GetFileName(source)}_${i}.ffd");
                        if (File.Exists(ffd))
                            File.Delete(ffd);
                        File.WriteAllBytes(ffd, featureContent);
                    }

                    Marshal.FreeHGlobal(faceModelPtr);
                    Marshal.FreeHGlobal(faceInputPtr);
                }
            }

            Marshal.FreeHGlobal(offInputPtr);
            Marshal.FreeHGlobal(imageDataPtr);

            return features;
        }

        private static byte[] ReadImage(Image image, out int width, out int height, out int pitch)
        {
            var bitmap = new Bitmap(image);
            //将Bitmap锁定到系统内存中,获得BitmapData
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
            var ptr = data.Scan0;
            //定义数组长度
            var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
            var sourceBitArray = new byte[sourceBitArrayLength];
            //将bitmap中的内容拷贝到ptr_bgr数组中
            Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);

            width = data.Width;
            height = data.Height;

            pitch = Math.Abs(data.Stride);

            var line = width * 3;
            var bgrLen = line * height;
            var destBitArray = new byte[bgrLen];

            for (var i = 0; i < height; ++i)
                Array.Copy(sourceBitArray, i * pitch, destBitArray, i * line, line);

            pitch = line;
            bitmap.UnlockBits(data);
            return destBitArray;
        }

        private Dictionary<string, float> Compare(byte[] feature)
        {
            var localFaceModels = new AFR_FSDK_FaceModel();
            var sourceFeaturePtr = Marshal.AllocHGlobal(feature.Length);
            Marshal.Copy(feature, 0, sourceFeaturePtr, feature.Length);
            localFaceModels.lFeatureSize = feature.Length;
            localFaceModels.pbFeature = sourceFeaturePtr;
            var firstPtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels));
            Marshal.StructureToPtr(localFaceModels, firstPtr, false);

            var dict = new Dictionary<string, float>();
            foreach (var ffd in _ffds)
            {
                if (!File.Exists(ffd))
                    continue;

                var libraryFeature = File.ReadAllBytes(ffd);
                var localFaceModels2 = new AFR_FSDK_FaceModel();
                var libraryFeaturePtr = Marshal.AllocHGlobal(libraryFeature.Length);
                Marshal.Copy(libraryFeature, 0, libraryFeaturePtr, libraryFeature.Length);
                localFaceModels2.lFeatureSize = libraryFeature.Length;
                localFaceModels2.pbFeature = libraryFeaturePtr;
                var secondPtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels2));
                Marshal.StructureToPtr(localFaceModels2, secondPtr, false);
                var result = 0f;
                AFRFunction.AFR_FSDK_FacePairMatching(_recognizeEngine, firstPtr, secondPtr, ref result);
                dict[ffd] = result;

                Marshal.FreeHGlobal(libraryFeaturePtr);
                Marshal.FreeHGlobal(secondPtr);
            }

            Marshal.FreeHGlobal(sourceFeaturePtr);
            Marshal.FreeHGlobal(firstPtr);
            return dict;
        }
    }
}