using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ColinChang.ArcFace.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace ColinChang.ArcFace
{
    public static class ArcFaceExtension
    {
        public static IServiceCollection AddArcFace(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configuration.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IArcFace, ArcFace>();
            return services;
        }

        public static IServiceCollection AddArcFace(this IServiceCollection services,
            Action<ArcFaceOptions> configureOptions)
        {
            services.AddOptions<ArcFaceOptions>()
                .Configure(configureOptions)
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
            try
            {
                var localFeature = new AsfFaceFeature { Feature = Marshal.AllocHGlobal(feature.Length) };
                Marshal.Copy(feature, 0, localFeature.Feature, feature.Length);
                localFeature.FeatureSize = feature.Length;

                var pLocalFeature = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFaceFeature>());
                Marshal.StructureToPtr(localFeature, pLocalFeature, false);
                return pLocalFeature;
            }
            catch (Exception e)
            {
                throw new InvalidFaceFeatureException(e);
            }
        }

        public static void AddFace(this ConcurrentDictionary<string, Face> faceLibrary, IEnumerable<Face> faces)
        {
            foreach (var face in faces)
                faceLibrary[face.Id] = face;
        }

        public static (bool Success, int SuccessCount) TryAddFace(this ConcurrentDictionary<string, Face> faceLibrary,
            IEnumerable<Face> faces)
        {
            var cnt = faces.Count(face => faceLibrary.TryAdd(face.Id, face));
            return (cnt >= faces.Count(), cnt);
        }

        public static void InitFaceLibrary(this ConcurrentDictionary<string, Face> faceLibrary, IEnumerable<Face> faces)
        {
            faceLibrary.Clear();
            faceLibrary.AddFace(faces);
        }

        public static (bool Success, int SuccessCount) TryInitFaceLibrary(
            this ConcurrentDictionary<string, Face> faceLibrary,
            IEnumerable<Face> faces)
        {
            faceLibrary.Clear();
            return faceLibrary.TryAddFace(faces);
        }


        /// <summary>
        /// 释放人脸指针及其指向的对象内存
        /// </summary>
        /// <param name="faceFeature"></param>
        public static void DisposeFaceFeature(this IntPtr faceFeature)
        {
            if (faceFeature == IntPtr.Zero)
                return;

            try
            {
                var memory = Marshal.PtrToStructure<AsfFaceFeature>(faceFeature);
                Marshal.FreeHGlobal(memory.Feature);
            }
            finally
            {
                Marshal.FreeHGlobal(faceFeature);
            }
        }

        public static async Task<Image> ToImage(this Stream image) => await Image.LoadAsync(image);

        public static async Task<Image> ToImage(this string image) => await Image.LoadAsync(image);
    }
}