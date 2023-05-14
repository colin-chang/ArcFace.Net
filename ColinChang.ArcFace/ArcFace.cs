using Microsoft.Extensions.Options;


namespace ColinChang.ArcFace
{
    public partial class ArcFace : IArcFace
    {
        private readonly ArcFaceOptions _options;

        public ArcFace(IOptionsMonitor<ArcFaceOptions> options) : this(options.CurrentValue)
        {
        }

        public ArcFace(ArcFaceOptions options)
        {
            _options = options;
            OnlineActiveAsync().Wait();
        }
    }
}