namespace ColinChang.FaceRecognition.Models
{
    /// <summary>
    /// 人脸框信息结构体
    /// </summary>
    public struct Rect
    {
        public int Top { get; set; }
        public int Bottom { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
    }
}