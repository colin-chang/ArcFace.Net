using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Extensions;
using SixLabors.ImageSharp;

namespace ColinChang.ArcFace.ImageSharp.Extensions;

public static class ImageSharpProcessorExtensions
{
    public static async Task<Image> ToImageAsync(this Stream image)
    {
        var img = await Image.LoadAsync(image);
        image.Reset();
        return img;
    }

    public static async Task<Stream> ToStreamAsync(this Image image)
    {
        var stream = new MemoryStream();
        await image.SaveAsync(stream, image.Metadata.DecodedImageFormat);
        stream.Reset();
        return stream;
    }
}