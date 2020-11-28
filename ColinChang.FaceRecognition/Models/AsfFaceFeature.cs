using System;
using System.Runtime.InteropServices;

namespace ColinChang.FaceRecognition.Models
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

    public static class FaceFeatureExtension
    {
        public static IntPtr ToFaceFeature(this byte[] feature)
        {
            var localFeature = new AsfFaceFeature {Feature = Marshal.AllocHGlobal(feature.Length)};
            Marshal.Copy(feature, 0, localFeature.Feature, feature.Length);
            localFeature.FeatureSize = feature.Length;

            var pLocalFeature = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFaceFeature>());
            Marshal.StructureToPtr(localFeature, pLocalFeature, false);
            return pLocalFeature;
        }
    }
}