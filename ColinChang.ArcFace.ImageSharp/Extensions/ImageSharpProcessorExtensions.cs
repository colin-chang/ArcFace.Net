using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace ColinChang.ArcFace.ImageSharp.Extensions;

public static class ImageSharpProcessorExtensions
{
    public static async Task<Image> ToImageAsync(this Stream image) => await Image.LoadAsync(image);

    public static async Task<Stream> ToStreamAsync(this Image image)
    {
        var stream = new MemoryStream();
        await image.SaveAsync(stream, image.Metadata.DecodedImageFormat);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}