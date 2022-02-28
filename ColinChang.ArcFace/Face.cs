using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace;

public class Face : IDisposable
{
    public Face(string id, IntPtr feature)
    {
        Id = id;
        Feature = feature;
    }

    public Face(string id, byte[] feature)
    {
        Id = id;
        FeatureBytes = feature;
    }

    public Face(string id, string feature)
    {
        Id = id;
        FeatureString = feature;
    }

    public Face(string id, string feature, object tag) : this(id, feature) =>
        Tag = tag;

    public string Id { get; set; }

    public IntPtr Feature { get; set; }

    private byte[] _featureBytes;

    public byte[] FeatureBytes
    {
        get => _featureBytes;
        set
        {
            _featureBytes = value;
            Feature = value.ToFaceFeature();
        }
    }

    /// <summary>
    /// base64 string of the feature
    /// </summary>
    public string FeatureString
    {
        get => FeatureBytes == null || !FeatureBytes.Any() ? null : Convert.ToBase64String(FeatureBytes);
        set => FeatureBytes = Convert.FromBase64String(value);
    }

    /// <summary>
    /// appended custom property 
    /// </summary>
    public object Tag { get; set; }

    public void Dispose() =>
        Marshal.FreeHGlobal(Feature);
}