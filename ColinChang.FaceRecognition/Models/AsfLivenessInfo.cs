using System;

namespace ColinChang.FaceRecognition.Models
{
    public struct AsfLivenessInfo
    {
        /// <summary>
        /// 是否是真人
        /// 0：非真人；1：真人；-1：不确定；-2:传入人脸数>1；
        /// </summary>
        public IntPtr IsLive { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int Num { get; set; }
    }
}