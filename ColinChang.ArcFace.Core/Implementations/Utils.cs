using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core.Utils;

namespace ColinChang.ArcFace.Core
{
    /// <summary>
    /// 工具方法
    /// </summary>
    public partial class ArcFace
    {
        private (ConcurrentQueue<IntPtr> Engines, int EnginesCount, EventWaitHandle EnginesWaitHandle) GetEngineStuff(
            DetectionModeEnum mode) =>
            mode switch
            {
                DetectionModeEnum.Image => (_imageEngines, ASF_IMAGE_ENGINES_COUNT,
                    _imageWaitHandle),
                DetectionModeEnum.Video => (_videoEngines, ASF_VIDEO_ENGINES_COUNT,
                    _videoWaitHandle),
                DetectionModeEnum.RGB => (_rgbEngines, ASF_RGB_ENGINES_COUNT, _rgbWaitHandle),
                DetectionModeEnum.IR => (_irEngines, ASF_IR_ENGINES_COUNT, _irWaitHandle),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "invalid detection mode")
            };


        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(Stream image,
            Func<IntPtr, ImageInfo, Task<OperationResult<T>>> process, DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            var engine = IntPtr.Zero;
            try
            {
                await using var imageInfo = await _processor.VerifyAsync(image);
                engine = GetEngine(mode);
                return (await process(engine, imageInfo)).Cast<TK>();
            }
            finally
            {
                RecycleEngine(engine, mode);
            }
        }

        private async Task<OperationResult<TK>> ProcessImageAsync<T, TK>(Stream image,
            Func<IntPtr, (ImageInfo, ImageInfo), Task<OperationResult<T>>> process,
            DetectionModeEnum mode = DetectionModeEnum.Image)
        {
            var engine = IntPtr.Zero;
            try
            {
                engine = GetEngine(mode);
                var (rgb, ir) = await _processor.VerifyIrAsync(image);
                await using (rgb)
                {
                    await using (ir)
                    {
                        return (await process(engine, (rgb, ir))).Cast<TK>();
                    }
                }
            }
            finally
            {
                RecycleEngine(engine, mode);
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
                    if (image is not Stream && image is not string)
                        throw new Exception("invalid images type");
                    if (image is string image0)
                    {
                        await using var img = image0.ToStream();
                        await using var imageInfo = await _processor.VerifyAsync(img);
                        var feature = await FaceHelper.ExtractSingleFeatureAsync(engine, imageInfo);
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
                        var img = image as Stream;
                        await using var imageInfo = await _processor.VerifyAsync(image as Stream);
                        var feature = await FaceHelper.ExtractSingleFeatureAsync(engine, imageInfo);
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