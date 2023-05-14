using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using ColinChang.ArcFace.Models;
using ColinChang.ArcFace.Utils;

namespace ColinChang.ArcFace
{
    /// <summary>
    /// 工具方法
    /// </summary>
    public partial class ArcFace
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

        private (ConcurrentQueue<IntPtr> Engines, int EnginesCount, EventWaitHandle EnginesWaitHandle) GetEngineStuff(
            DetectionModeEnum mode) =>
            mode switch
            {
                DetectionModeEnum.Image => (_imageEngines, _imageEnginesCount: ASF_IMAGE_ENGINES_COUNT,
                    _imageWaitHandle),
                DetectionModeEnum.Video => (_videoEngines, _videoEnginesCount: ASF_VIDEO_ENGINES_COUNT,
                    _videoWaitHandle),
                DetectionModeEnum.RGB => (_rgbEngines, _rgbEnginesCount: ASF_RGB_ENGINES_COUNT, _rgbWaitHandle),
                DetectionModeEnum.IR => (_irEngines, _irEnginesCount: ASF_IR_ENGINES_COUNT, _irWaitHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "invalid detection mode")
            };

        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(string image,
            Func<IntPtr, Image, Task<OperationResult<T>>> process, DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            if (!File.Exists(image))
                return new OperationResult<TK>(default);

            using var img = await image.ToImage();
            return await ProcessImageAsync<T, TK>(img, process, mode);
        }

        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(Image image,
            Func<IntPtr, Image, Task<OperationResult<T>>> process, DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            var engine = IntPtr.Zero;
            try
            {
                VerifyImage(image);
                engine = GetEngine(mode);
                return (await process(engine, image)).Cast<TK>();
            }
            finally
            {
                RecycleEngine(engine, mode);
            }
        }

        private void VerifyImage(Image image)
        {
            if (image == null)
                throw new FileNotFoundException("image cannot be null.");

            using var stream = new MemoryStream();
            image.Save(stream, image.Metadata.DecodedImageFormat);
            var length = stream.Length;
            if (length > ASF_MAX_IMAGE_SIZE)
                throw new Exception($"image is oversize than {ASF_MAX_IMAGE_SIZE}B.");
            if (length < ASF_MIN_IMAGE_SIZE)
                throw new Exception($"image is too small than {ASF_MIN_IMAGE_SIZE}B");

            if (!_supportedImageExtensions.Contains($".{image.Metadata.DecodedImageFormat.Name.ToLower()}"))
                throw new Exception("unsupported image type.");

            ScaleImage(image);
        }

        private static void ScaleImage(Image image)
        {
            //缩放
            if (image.Width > 1536 || image.Height > 1536)
                image.ScaleImage(1536, 1536);
            if (image.Width % 4 != 0)
                image.ScaleImage(image.Width - image.Width % 4, image.Height);
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
                        if (!File.Exists(image0))
                            throw new FileNotFoundException($"{image} doesn't exist.");

                        using var img = await image0.ToImage();
                        VerifyImage(img);
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
                        var img = image as Image;
                        VerifyImage(image as Image);
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
    }
}