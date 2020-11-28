using System;

namespace ColinChang.FaceRecognition.Models
{
    /// <summary>
    /// 视频检测缓存实体类
    /// </summary>
    public class FaceTrackUnit
    {
        public Rect Rect { get; set; }
        public IntPtr Feature { get; set; }
        public string Message { get; set; }
    }
}