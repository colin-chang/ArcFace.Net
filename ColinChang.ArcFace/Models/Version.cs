using System;

namespace ColinChang.ArcFace.Models
{
    internal struct AsfVersionInfo : ICast<VersionInfo>
    {
        public string Version { get; set; }
        public string BuildDate { get; set; }
        public string CopyRight { get; set; }

        public VersionInfo Cast()
        {
            return new VersionInfo(Version, DateTime.Parse(BuildDate), CopyRight);
        }
    }

    /// <summary>
    /// SDK版本信息结构体
    /// </summary>
    public struct VersionInfo
    {
        public string Version { get; set; }
        public DateTime BuildDate { get; set; }
        public string CopyRight { get; set; }

        public VersionInfo(string version, DateTime buildDate, string copyRight)
        {
            Version = version;
            BuildDate = buildDate;
            CopyRight = copyRight;
        }
    }
}