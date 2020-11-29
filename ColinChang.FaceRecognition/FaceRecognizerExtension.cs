using System;
using System.Runtime.InteropServices;
using ColinChang.FaceRecognition.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ColinChang.FaceRecognition
{
    public static class FaceRecognizerExtension
    {
        public static IServiceCollection AddFaceRecognizer(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<FaceRecognitionOptions>()
                .Configure(config.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IFaceRecognizer, FaceRecognizer>();
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