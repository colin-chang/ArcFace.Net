using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Abstraction.Extensions;

namespace ColinChang.ArcFace.SystemDrawing;

public class SystemDrawingProcessor : IImageProcessor
{
    public async Task<Stream> ScaleAsync(Stream image, int dstWidth, int dstHeight)
    {
        using var img = Image.FromStream(image);
        image.Reset();

        //按比例缩放           
        var scaleRate = GetWidthAndHeight(img.Width, img.Height, dstWidth, dstHeight);
        var width = (int)(img.Width * scaleRate);
        var height = (int)(img.Height * scaleRate);

        //将宽度调整为4的整数倍
        if (width % 4 != 0)
            width -= width % 4;

        using var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);

        //设置画布的描绘质量         
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(img, new Rectangle((width - width) / 2, (height - height) / 2, width, height), 0, 0,
            img.Width, img.Height, GraphicsUnit.Pixel);

        var stream = new MemoryStream();
        bitmap.Save(stream, img.RawFormat);
        stream.Reset();
        return await Task.FromResult(stream);
    }

    public Task<string> GetFormatAsync(Stream image)
    {
        using var img = Image.FromStream(image);
        image.Reset();
        return Task.FromResult(img.RawFormat.ToString().ToLower());
    }

    public async Task<ImageInfo> GetImageInfoAsync(Stream image)
    {
        //将Image转换为Format24bppRgb格式的BMP
        var bm = new Bitmap(image);
        image.Reset();
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
            return await Task.FromResult(imageInfo);
        }
        finally
        {
            bm.UnlockBits(data);
            bm.Dispose();
        }
    }

    public Task<ImageInfo> GetIrImageInfoAsync(Stream image)
    {
        var imageInfo = new ImageInfo();
        var img = new Bitmap(image);
        var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly,
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
                destBitArray[j++] = (byte)colorTemp;
            }

            imageInfo.ImgData = Marshal.AllocHGlobal(destBitArray.Length);
            Marshal.Copy(destBitArray, 0, imageInfo.ImgData, destBitArray.Length);

            return Task.FromResult(imageInfo);
        }
        finally
        {
            img.UnlockBits(data);
            img.Dispose();
        }
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
}