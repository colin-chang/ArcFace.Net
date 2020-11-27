namespace ColinChang.FaceRecognition.Models
{
    /// <summary>
    /// 单人脸检测结构体
    /// </summary>
    public struct ASF_SingleFaceInfo
    {
        /// <summary>
        /// 人脸坐标Rect结果
        /// </summary>
        public Rect faceRect;
        
        /// <summary>
        /// 人脸角度
        /// </summary>
        public int faceOrient;
    }
}
