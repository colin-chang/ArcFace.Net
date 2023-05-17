using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Models;

namespace ColinChang.ArcFace.Abstraction;

public interface IImageProcessor
{
    /// <summary>
    /// 缩放图片
    /// </summary>
    /// <param name="image">原图片</param>
    /// <param name="dstWidth">目标图片宽</param>
    /// <param name="dstHeight">目标图片高</param>
    Task<Stream> ScaleAsync(Stream image, int dstWidth, int dstHeight);

    /// <summary>
    /// 获取图片格式
    /// </summary>
    /// <param name="image">原图片</param>
    /// <returns></returns>
    Task<string> GetFormatAsync(Stream image);

    /// <summary>
    /// 获取图片信息
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>图片信息</returns>
    Task<ImageInfo> GetImageInfoAsync(Stream image);

    /// <summary>
    /// 获取图片IR信息
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>图片IR信息</returns>
    Task<ImageInfo> GetIrImageInfoAsync(Stream image);
}