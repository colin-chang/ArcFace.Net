using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Models;

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
        public static Image ScaleImage(Image image, int dstWidth, int dstHeight)
        {
            //按比例缩放           
            var scaleRate = GetWidthAndHeight(image.Width, image.Height, dstWidth, dstHeight);
            var width = (int) (image.Width * scaleRate);
            var height = (int) (image.Height * scaleRate);

            //将宽度调整为4的整数倍
            if (width % 4 != 0)
                width -= width % 4;

            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);

            //设置画布的描绘质量         
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, new Rectangle((width - width) / 2, (height - height) / 2, width, height), 0, 0,
                image.Width, image.Height, GraphicsUnit.Pixel);

            return bitmap;
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

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="image">图片</param>
        /// <returns></returns>
        public static ImageInfo ReadBmp(Image image)
        {
            //将Image转换为Format24bppRgb格式的BMP
            var bm = new Bitmap(image);
            var data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            try
            {
                //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
                var ptr = data.Scan0;

                //定义数组长度
                var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
                var sourceBitArray = new byte[sourceBitArrayLength];

                //将bitmap中的内容拷贝到ptr_bgr数组中
                Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);

                //填充引用对象字段值
                var imageInfo = new ImageInfo
                {
                    Width = data.Width,
                    Height = data.Height,
                    Format = AsfImagePixelFormat.ASVL_PAF_RGB24_B8G8R8,
                    ImgData = Marshal.AllocHGlobal(sourceBitArray.Length)
                };

                Marshal.Copy(sourceBitArray, 0, imageInfo.ImgData, sourceBitArray.Length);
                return imageInfo;
            }
            finally
            {
                bm.UnlockBits(data);
                bm.Dispose();
            }
        }

        /// <summary>
        /// 获取图片IR信息
        /// </summary>
        /// <param name="image">图片</param>
        /// <returns>成功或失败</returns>
        public static ImageInfo ReadBMP_IR(Bitmap image)
        {
            var imageInfo = new ImageInfo();

            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            try
            {
                //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
                var ptr = data.Scan0;

                //定义数组长度
                var sourceBitArrayLength = data.Height * Math.Abs(data.Stride);
                var sourceBitArray = new byte[sourceBitArrayLength];

                //将bitmap中的内容拷贝到ptr_bgr数组中
                Marshal.Copy(ptr, sourceBitArray, 0, sourceBitArrayLength);

                //填充引用对象字段值
                imageInfo.Width = data.Width;
                imageInfo.Height = data.Height;
                imageInfo.Format = AsfImagePixelFormat.ASVL_PAF_GRAY;

                //获取去除对齐位后度图像数据
                var line = imageInfo.Width;
                var pitch = Math.Abs(data.Stride);
                var irLen = line * imageInfo.Height;
                var destBitArray = new byte[irLen];

                //灰度化
                var j = 0;
                for (var i = 0; i < sourceBitArray.Length; i += 3)
                {
                    var colorTemp = sourceBitArray[i + 2] * 0.299 + sourceBitArray[i + 1] * 0.587 +
                                    sourceBitArray[i] * 0.114;
                    destBitArray[j++] = (byte) colorTemp;
                }

                imageInfo.ImgData = Marshal.AllocHGlobal(destBitArray.Length);
                Marshal.Copy(destBitArray, 0, imageInfo.ImgData, destBitArray.Length);

                return imageInfo;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }

        /// <summary>
        /// 剪裁图片
        /// </summary>
        /// <param name="src">原图片</param>
        /// <param name="left">左坐标</param>
        /// <param name="top">顶部坐标</param>
        /// <param name="right">右坐标</param>
        /// <param name="bottom">底部坐标</param>
        /// <returns>剪裁后的图片</returns>
        public static Image CutImage(Image src, int left, int top, int right, int bottom)
        {
            try
            {
                var srcBitmap = new Bitmap(src);
                var width = right - left;
                var height = bottom - top;
                var destBitmap = new Bitmap(width, height);
                using var g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);

                //设置画布的描绘质量         
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(srcBitmap, new Rectangle(0, 0, width, height), left, top, width, height,
                    GraphicsUnit.Pixel);

                return destBitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}