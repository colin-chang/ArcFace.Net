using System;

namespace ColinChang.FaceRecognition.Models
{
    struct AsfActiveFileInfo
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Platform { get; set; }
        public string SdkType { get; set; }
        public string AppId { get; set; }
        public string SdkKey { get; set; }
        public string SdkVersion { get; set; }
        public string FileVersion { get; set; }

        public ActiveFileInfo Cast() =>
            new ActiveFileInfo(
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(StartTime)).LocalDateTime,
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(EndTime)).LocalDateTime,
                Platform, SdkType, AppId, SdkKey, SdkVersion, FileVersion);
    }

    public struct ActiveFileInfo
    {
        public DateTime StartTime { get; set; } //开始时间
        public DateTime EndTime { get; set; } //截止时间
        public string Platform { get; set; } //平台
        public string SdkType { get; set; } //sdk类型
        public string AppId { get; set; } //APPID
        public string SdkKey { get; set; } //SDKKEY
        public string SdkVersion { get; set; } //SDK版本号
        public string FileVersion { get; set; } //激活文件版本号

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