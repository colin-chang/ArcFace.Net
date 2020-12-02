using System;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace.Models
{
    public struct AsfGenderInfo : ICast<GenderInfo>
    {
        public IntPtr GenderArray { get; set; }

        public int Num { get; set; }

        public GenderInfo Cast()
        {
            var genders = new Gender[Num];
            var size = Marshal.SizeOf<int>();
            for (var i = 0; i < Num; i++)
                genders[i] = (Gender) Marshal.ReadInt32(GenderArray, i * size);

            return new GenderInfo(genders, Num);
        }
    }

    /// <summary>
    /// 性别结构体
    /// </summary>
    public struct GenderInfo
    {
        /// <summary>
        /// 性别检测结果集合
        /// </summary>
        public Gender[] GenderArray { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int Num { get; set; }

        public GenderInfo(Gender[] genderArray, int num)
        {
            GenderArray = genderArray;
            Num = num;
        }
    }

    public enum Gender
    {
        Unknown = -1,
        Male = 0,
        Female = 1
    }
}