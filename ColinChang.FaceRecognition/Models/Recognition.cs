namespace ColinChang.FaceRecognition.Models
{
    public class Recognition
    {
        /// <summary>
        /// 人脸ID
        /// </summary>
        public string FaceId { get; set; }

        /// <summary>
        /// 相似度
        /// </summary>
        public float Similarity { get; set; }
    }
}