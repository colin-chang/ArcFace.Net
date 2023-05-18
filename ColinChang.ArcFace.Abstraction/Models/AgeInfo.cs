using System;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace.Abstraction.Models
{
    public struct AsfAgeInfo : ICast<AgeInfo>
    {
        public IntPtr AgeArray { get; set; }
        public int Num { get; set; }

        public AgeInfo Cast()
        {
            var ages = new int[Num];
            var size = Marshal.SizeOf<int>();
            for (var i = 0; i < Num; i++)
                ages[i] = Marshal.ReadInt32(AgeArray, i * size);

            return new AgeInfo(ages, Num);
        }
    }

    /// <summary>
    /// 年龄信息
    /// </summary>
    public struct AgeInfo
    {
        /// <summary>
        /// 年龄检测结果集合
        /// </summary>
        public int[] AgeArray { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int Num { get; set; }

        public AgeInfo(int[] ageArray, int num)
        {
            AgeArray = ageArray;
            Num = num;
        }
    }
}