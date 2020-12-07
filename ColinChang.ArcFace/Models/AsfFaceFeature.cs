using System;

namespace ColinChang.ArcFace.Models
{
    /// <summary>
    /// 人脸特征结构体
    /// </summary>
    public struct AsfFaceFeature
    {
        /// <summary>
        /// 特征值
        /// </summary>
        public IntPtr Feature { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int FeatureSize { get; set; }
    }
}