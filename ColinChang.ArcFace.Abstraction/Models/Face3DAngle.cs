using System;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace.Abstraction.Models
{
    public struct AsfFace3DAngle : ICast<Face3DAngle>
    {
        public IntPtr Roll { get; set; }
        public IntPtr Yaw { get; set; }
        public IntPtr Pitch { get; set; }
        public IntPtr Status { get; set; }
        public int Num { get; set; }

        public Face3DAngle Cast()
        {
            var roll = new float[Num];
            var yaw = new float[Num];
            var pitch = new float[Num];
            var status = new int[Num];

            var floatSize = Marshal.SizeOf<float>();
            var intSize = Marshal.SizeOf<int>();
            for (var i = 0; i < Num; i++)
            {
                var pointer = Roll + i * floatSize;
                roll[i] = Marshal.PtrToStructure<float>(pointer);
                pointer = Yaw + i * floatSize;
                yaw[i] = Marshal.PtrToStructure<float>(pointer);
                pointer = Pitch + i * floatSize;
                pitch[i] = Marshal.PtrToStructure<float>(pointer);
                status[i] = Marshal.ReadInt32(Status, i * intSize);
            }

            return new Face3DAngle(roll, yaw, pitch, status, Num);
        }
    }

    /// <summary>
    /// 3D人脸角度检测结构体，可参考https://ai.arcsoft.com.cn/bbs/forum.php?mod=viewthread&tid=1459&page=1&extra=&_dsign=fd9e1a7a
    /// </summary>
    public struct Face3DAngle
    {
        public float[] Roll { get; set; }
        public float[] Yaw { get; set; }
        public float[] Pitch { get; set; }

        /// <summary>
        /// 是否检测成功，0成功，其他为失败
        /// </summary>
        public int[] Status { get; set; }

        public int Num { get; set; }

        public Face3DAngle(float[] roll, float[] yaw, float[] pitch, int[] status, int num)
        {
            Roll = roll;
            Yaw = yaw;
            Pitch = pitch;
            Status = status;
            Num = num;
        }
    }
}