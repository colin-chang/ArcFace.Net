using System.IO;
using System.Threading.Tasks;
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

    public static void Reset(this Stream stream)
    {
        if (stream == null)
            return;
        if (stream.Position > 0)
            stream.Seek(0, SeekOrigin.Begin);
    }
}