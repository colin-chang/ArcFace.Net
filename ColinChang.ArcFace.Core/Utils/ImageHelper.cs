using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;

namespace ColinChang.ArcFace.Core.Utils
{
    static class ImageHelper
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
        /// 图像最大宽高
        /// </summary>
        private const int ASF_MAX_IMAGE_WIDTH_HEIGHT = 1536;

        /// <summary>
        /// 支持的图片格式
        /// </summary>
        private static readonly string[] SupportedImageExtensions = { ".jpeg", ".jpg", ".png", ".bmp" };

        #endregion

        /// <summary>
        /// 验证图片
        /// 校验->缩放->获取信息
        /// </summary>
        /// <param name="processor">图片处理器</param>
        /// <param name="image">图像</param>
        /// <returns>图片信息</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<ImageInfo> VerifyAsync(this IImageProcessor processor, Stream image)
        {
            await using var img = await processor.VerifyAndScaleAsync(image);
            return await processor.GetImageInfoAsync(img);
        }

        public static async Task<(ImageInfo RGB, ImageInfo IR)> VerifyIrAsync(this IImageProcessor processor,
            Stream image)
        {
            await using var img = await processor.VerifyAndScaleAsync(image);
            var rgb = await processor.GetImageInfoAsync(img);
            var ir = await processor.GetIrImageInfoAsync(img);
            return (rgb, ir);
        }

        /// <summary>
        /// 校验并缩放图片
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        private static async Task<Stream> VerifyAndScaleAsync(this IImageProcessor processor, Stream image)
        {
            if (image is not { Length: > 0 })
                throw new FileNotFoundException("image cannot be null.");

            if (image.Length > ASF_MAX_IMAGE_SIZE)
                throw new Exception($"image is oversize than {ASF_MAX_IMAGE_SIZE}B.");
            if (image.Length < ASF_MIN_IMAGE_SIZE)
                throw new Exception($"image is too small than {ASF_MIN_IMAGE_SIZE}B");

            image.Reset();
            var format = await processor.GetFormatAsync(image);
            if (!SupportedImageExtensions.Contains($".{format}"))
                throw new Exception("unsupported image type.");

            return await ScaleAsync(image, processor);
        }

        /// <summary>
        /// 缩放图片
        /// </summary>
        /// <param name="image"></param>
        /// <param name="processor"></param>
        /// <returns></returns>
        private static async Task<Stream> ScaleAsync(Stream image, IImageProcessor processor)
        {
            await using var imageInfo = await processor.GetImageInfoAsync(image);

            Stream img = null;
            if (imageInfo.Width > ASF_MAX_IMAGE_WIDTH_HEIGHT || imageInfo.Height > ASF_MAX_IMAGE_WIDTH_HEIGHT)
                img = await processor.ScaleAsync(image, ASF_MAX_IMAGE_WIDTH_HEIGHT, ASF_MAX_IMAGE_WIDTH_HEIGHT);
            if (imageInfo.Width % 4 == 0)
            {
                if (img != null)
                    return img;
                        
                var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                image.Reset();
                stream.Reset();
                return stream;
            }

            if (img==null)
                return await processor.ScaleAsync(image, imageInfo.Width - imageInfo.Width % 4, imageInfo.Height);
            
            await using (img)
            {
                return await processor.ScaleAsync(img, imageInfo.Width - imageInfo.Width % 4, imageInfo.Height);
            }
        }
    }
}