using SixLabors.ImageSharp;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Models;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Threading.Tasks;

namespace ColinChang.ArcFace.Utils
{
    internal static class ImageHelper
    {
        /// <summary>
        /// 按指定宽高缩放图片
        /// </summary>
        /// <param name="image">原图片</param>
        /// <param name="dstWidth">目标图片宽</param>
        /// <param name="dstHeight">目标图片高</param>
        /// <returns></returns>
        public static void ScaleImage(this Image image, int dstWidth, int dstHeight)
        {
            //按比例缩放           
            var scaleRate = GetWidthAndHeight(image.Width, image.Height, dstWidth, dstHeight);
            var width = (int)(image.Width * scaleRate);
            var height = (int)(image.Height * scaleRate);

            //将宽度调整为4的整数倍
            if (width % 4 != 0)
                width -= width % 4;

            image.Mutate(x => x.Resize(width, height));
        }

        /// <summary>
        /// 获取图片缩放比例
        /// </summary>
        /// <param name="oldWidth">原图片宽</param>
        /// <param name="oldHeight">原图片高</param>
        /// <param name="newWidth">目标图片宽</param>
        /// <param name="newHeight">目标图片高</param>
        /// <returns></returns>
        private static float GetWidthAndHeight(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            //按比例缩放           
            float scaleRate;
            if (oldWidth >= newWidth && oldHeight >= newHeight)
            {
                var widthDis = oldWidth - newWidth;
                var heightDis = oldHeight - newHeight;

                scaleRate = widthDis > heightDis ? newWidth * 1f / oldWidth : newHeight * 1f / oldHeight;
            }
            else if (oldWidth >= newWidth && oldHeight < newHeight)
            {
                scaleRate = newWidth * 1f / oldWidth;
            }
            else if (oldWidth < newWidth && oldHeight >= newHeight)
            {
                scaleRate = newHeight * 1f / oldHeight;
            }
            else
            {
                var widthDis = newWidth - oldWidth;
                var heightDis = newHeight - oldHeight;
                if (widthDis > heightDis)
                    scaleRate = newHeight * 1f / oldHeight;
                else
                    scaleRate = newWidth * 1f / oldWidth;
            }

            return scaleRate;
        }


        //public static Models.ImageInfo ReadBmp(string image)
        //{
        //    //将Image转换为Format24bppRgb格式的BMP
        //    var bm = new Bitmap(System.Drawing.Image.FromFile(image));
        //    var data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly,
        //        PixelFormat.Format24bppRgb);
        //    try
        //    {
        //        //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
        //        var ptr = data.Scan0;

        //        //定义数组长度
        //        var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
        //        var sourceBitArray = new byte[sourceBitArrayLength];

        //        //将bitmap中的内容拷贝到ptr_bgr数组中
        //        Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);

        //        //填充引用对象字段值
        //        var imageInfo = new Models.ImageInfo
        //        {
        //            Width = data.Width,
        //            Height = data.Height,
        //            Format = AsfImagePixelFormat.ASVL_PAF_RGB24_B8G8R8,
        //            ImgData = Marshal.AllocHGlobal(sourceBitArray.Length)
        //        };

        //        Marshal.Copy(sourceBitArray, 0, imageInfo.ImgData, sourceBitArray.Length);
        //        return imageInfo;
        //    }
        //    finally
        //    {
        //        bm.UnlockBits(data);
        //        bm.Dispose();
        //    }
        //}


        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="image">图片</param>
        /// <returns></returns>
        public static async Task<Models.ImageInfo> ReadBmpAsync(this Image image)
        {
            using var stream = new MemoryStream();
            await image.SaveAsync(stream, image.Metadata.DecodedImageFormat);
            stream.Seek(0, SeekOrigin.Begin);
            using var img = await Image.LoadAsync<Rgb24>(stream);

            var sourceBitArrayLength = img.Width * img.Height * 3;
            var sourceBitArray = new byte[sourceBitArrayLength];

            img.CopyPixelDataTo(sourceBitArray);

            var ii = new Models.ImageInfo
            {
                Width = img.Width,
                Height = img.Height,
                Format = AsfImagePixelFormat.ASVL_PAF_RGB24_B8G8R8,
                ImgData = Marshal.AllocHGlobal(sourceBitArrayLength)
            };
            Marshal.Copy(sourceBitArray, 0, ii.ImgData, sourceBitArrayLength);
            return ii;
        }

        //public static Models.ImageInfo ReadBMP_IR(string src)
        //{
        //    var imageInfo = new Models.ImageInfo();

        //    using var image = new Bitmap(System.Drawing.Image.FromFile(src));
        //    var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
        //        PixelFormat.Format24bppRgb);
        //    try
        //    {
        //        //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
        //        var ptr = data.Scan0;

        //        //定义数组长度
        //        var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
        //        var sourceBitArray = new byte[sourceBitArrayLength];

        //        //将bitmap中的内容拷贝到ptr_bgr数组中
        //        Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);

        //        //填充引用对象字段值
        //        imageInfo.Width = data.Width;
        //        imageInfo.Height = data.Height;
        //        imageInfo.Format = AsfImagePixelFormat.ASVL_PAF_GRAY;

        //        //获取去除对齐位后度图像数据
        //        var line = imageInfo.Width;
        //        var irLen = line * imageInfo.Height;
        //        var destBitArray = new byte[irLen];

        //        //灰度化
        //        var j = 0;
        //        for (var i = 0; i < sourceBitArray.Length; i += 3)
        //        {
        //            var colorTemp = sourceBitArray[i + 2] * 0.299 + sourceBitArray[i + 1] * 0.587 +
        //                            sourceBitArray[i] * 0.114;
        //            destBitArray[j++] = (byte)colorTemp;
        //        }

        //        imageInfo.ImgData = Marshal.AllocHGlobal(destBitArray.Length);
        //        Marshal.Copy(destBitArray, 0, imageInfo.ImgData, destBitArray.Length);

        //        return imageInfo;
        //    }
        //    finally
        //    {
        //        image.UnlockBits(data);
        //    }
        //}

        public async static Task<Models.ImageInfo> ReadBMP_IRAsync(Image image)
        {
            using var stream = new MemoryStream();
            await image.SaveAsync(stream, image.Metadata.DecodedImageFormat);
            stream.Seek(0, SeekOrigin.Begin);
            using var img = await Image.LoadAsync<Rgb24>(stream);

            var destBitArrayLength = img.Width * img.Height;
            var destBitArray = new byte[destBitArrayLength];
            var sourceBitArrayLength = destBitArrayLength * 3;
            var sourceBitArray = new byte[sourceBitArrayLength];
            img.CopyPixelDataTo(sourceBitArray);

            var imageInfo = new Models.ImageInfo
            {
                Width = img.Width,
                Height = img.Height,
                Format = AsfImagePixelFormat.ASVL_PAF_GRAY,
                ImgData = Marshal.AllocHGlobal(destBitArrayLength)
            };


            //灰度化
            var j = 0;
            for (var i = 0; i < sourceBitArray.Length; i += 3)
            {
                var colorTemp = sourceBitArray[i + 2] * 0.299 + sourceBitArray[i + 1] * 0.587 +
                                sourceBitArray[i] * 0.114;
                destBitArray[j++] = (byte)colorTemp;
            }

            Marshal.Copy(destBitArray, 0, imageInfo.ImgData, destBitArray.Length);
            return imageInfo;
        }

        /// <summary>
        /// 剪裁图片
        /// </summary>
        /// <param name="image">原图片</param>
        /// <param name="left">左坐标</param>
        /// <param name="top">顶部坐标</param>
        /// <param name="right">右坐标</param>
        /// <param name="bottom">底部坐标</param>
        /// <returns>剪裁后的图片</returns>
        public static Image CutImage(Image image, int left, int top, int right, int bottom)
        {
            try
            {
                //using var image = SixLabors.ImageSharp.Image.Load(src);
                var width = right - left;
                var height = bottom - top;
                using var destImage = new Image<Rgba32>(width, height);
                destImage.Mutate(ctx => ctx.DrawImage(image, new Rectangle(0, 0, width, height), 1));

                //var memoryStream = new MemoryStream();
                //destImage.Save(memoryStream, new JpegEncoder());
                //memoryStream.Seek(0, SeekOrigin.Begin);
                //return Image.FromStream(memoryStream);
                return destImage;
            }
            catch
            {
                return null;
            }
        }
    }
}