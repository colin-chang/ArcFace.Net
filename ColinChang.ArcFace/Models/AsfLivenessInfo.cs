using System;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace.Models
{
    public struct AsfLivenessInfo : ICast<LivenessInfo>
    {
        public IntPtr IsLive { get; set; }

        public int Num { get; set; }

        public LivenessInfo Cast()
        {
            var size = Marshal.SizeOf<int>();
            var isLive = new Liveness[Num];
            for (var i = 0; i < Num; i++)
                isLive[i] = (Liveness) Marshal.ReadInt32(IsLive, i * size);

            return new LivenessInfo(isLive, Num);
        }
    }

    /// <summary>
    /// 活体信息
    /// </summary>
    public struct LivenessInfo
    {
        /// <summary>
        /// 是否是真人
        /// 0：非真人；1：真人；-1：不确定；-2:传入人脸数>1；
        /// </summary>
        public Liveness[] IsLive { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int Num { get; set; }

        public LivenessInfo(Liveness[] isLive, int num)
        {
            IsLive = isLive;
            Num = num;
        }
    }

    public enum Liveness
    {
        MultipleFace = -2,
        Unknown = -1,
        No = 0,
        Yes = 1
    }

    public enum LivenessMode
    {
        /// <summary>
        /// RGB可见光活体
        /// </summary>
        RGB,

        /// <summary>
        /// IR红外活体
        /// </summary>
        IR
    }
}