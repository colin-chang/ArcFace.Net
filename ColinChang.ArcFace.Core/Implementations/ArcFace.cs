using Microsoft.Extensions.Options;
using ColinChang.ArcFace.Abstraction;

namespace ColinChang.ArcFace.Core;

public partial class ArcFace : IArcFace
{
    private IImageProcessor _processor;
    private readonly ArcFaceOptions _options;

    public ArcFace(IImageProcessor processor, IOptionsMonitor<ArcFaceOptions> options) : this(processor,
        options.CurrentValue)
    {
    }

    public ArcFace(IImageProcessor processor, ArcFaceOptions options)
    {
        _processor = processor;
        _options = options;
        OnlineActiveAsync().Wait();
    }
}