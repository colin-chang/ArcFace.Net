using System;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ColinChang.ArcFace
{
    public static class ArcFaceExtension
    {
        public static IServiceCollection AddArcFace(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(config.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, ArcFace>();
            return services;
        }

        /// <summary>
        /// 将字节数组转为人脸特征指针
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
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