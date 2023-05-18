using System;

namespace ColinChang.ArcFace.Abstraction.Models
{
    public struct AsfActiveFileInfo : ICast<ActiveFileInfo>
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Platform { get; set; }
        public string SdkType { get; set; }
        public string AppId { get; set; }
        public string SdkKey { get; set; }
        public string SdkVersion { get; set; }
        public string FileVersion { get; set; }

        public ActiveFileInfo Cast()
        {
            return new ActiveFileInfo(
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(StartTime)).LocalDateTime,
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(EndTime)).LocalDateTime,
                Platform, SdkType, AppId, SdkKey, SdkVersion, FileVersion);
        }
    }

    /// <summary>
    /// 激活信息
    /// </summary>
    public struct ActiveFileInfo
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 截止时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 平台
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// sdk类型
        /// </summary>
        public string SdkType { get; set; }

        /// <summary>
        /// APP_ID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// SDK_KEY
        /// </summary>
        public string SdkKey { get; set; }

        /// <summary>
        /// SDK版本号
        /// </summary>
        public string SdkVersion { get; set; }

        /// <summary>
        /// 激活文件版本号
        /// </summary>
        public string FileVersion { get; set; }

        /// <summary>
        /// 激活信息
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="platform"></param>
        /// <param name="sdkType"></param>
        /// <param name="appId"></param>
        /// <param name="sdkKey"></param>
        /// <param name="sdkVersion"></param>
        /// <param name="fileVersion"></param>
        public ActiveFileInfo(DateTime startTime, DateTime endTime, string platform, string sdkType, string appId,
            string sdkKey, string sdkVersion, string fileVersion)
        {
            StartTime = startTime;
            EndTime = endTime;
            Platform = platform;
            SdkType = sdkType;
            AppId = appId;
            SdkKey = sdkKey;
            SdkVersion = sdkVersion;
            FileVersion = fileVersion;
        }
    }
}