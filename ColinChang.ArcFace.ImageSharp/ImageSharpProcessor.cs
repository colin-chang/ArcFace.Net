using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.ImageSharp.Extensions;
using ImageInfo = ColinChang.ArcFace.Abstraction.Models.ImageInfo;

namespace ColinChang.ArcFace.ImageSharp;

public class ImageSharpProcessor : IImageProcessor
{
    public async Task<Stream> ScaleAsync(Stream image, int dstWidth, int dstHeight)
    {
        using var img = await image.ToImageAsync();
        //按比例缩放           
        var scaleRate = GetWidthAndHeight(img.Width, img.Height, dstWidth, dstHeight);
        var width = (int)(img.Width * scaleRate);
        var height = (int)(img.Height * scaleRate);

        //将宽度调整为4的整数倍
        if (width % 4 != 0)
            width -= width % 4;

        img.Mutate(x => x.Resize(width, height));
        return await img.ToStreamAsync();
    }

    public async Task<string> GetFormatAsync(Stream image)
    {
        using var img = await image.ToImageAsync();
        return img.Metadata.DecodedImageFormat?.Name.ToLower();
    }


    public async Task<ImageInfo> GetImageInfoAsync(Stream image)
    {
        using var img = await Image.LoadAsync<Rgb24>(image);
        image.Reset();
        var sourceBitArrayLength = img.Width * img.Height * 3;
        var sourceBitArray = new byte[sourceBitArrayLength];

        img.CopyPixelDataTo(sourceBitArray);

        var imageInfo = new ImageInfo
        {
            Width = img.Width,
            Height = img.Height,
            Format = AsfImagePixelFormat.ASVL_PAF_RGB24_B8G8R8,
            ImgData = Marshal.AllocHGlobal(sourceBitArrayLength)
        };
        Marshal.Copy(sourceBitArray, 0, imageInfo.ImgData, sourceBitArrayLength);
        return imageInfo;
    }

    public async Task<ImageInfo> GetIrImageInfoAsync(Stream image)
    {
        using var img = await Image.LoadAsync<Rgb24>(image);
        var destBitArrayLength = img.Width * img.Height;
        var destBitArray = new byte[destBitArrayLength];
        var sourceBitArrayLength = destBitArrayLength * 3;
        var sourceBitArray = new byte[sourceBitArrayLength];
        img.CopyPixelDataTo(sourceBitArray);

        var imageInfo = new ImageInfo
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