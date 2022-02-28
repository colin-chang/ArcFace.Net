using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        public static void AddFace(this ConcurrentDictionary<string, Face> faceLibrary, Face face) =>
            faceLibrary[face.Id] = face;

        public static bool TryAddFace(this ConcurrentDictionary<string, Face> faceLibrary, Face face) =>
            faceLibrary.TryAdd(face.Id, face);

        public static void AddFaces(this ConcurrentDictionary<string, Face> faceLibrary, IEnumerable<Face> faces)
        {
            if (faces == null || !faces.Any())
                return;

            foreach (var face in faces)
                faceLibrary.AddFace(face);
        }

        public static bool TryAddFaces(this ConcurrentDictionary<string, Face> faceLibrary, IEnumerable<Face> faces)
        {
            if (faces == null || !faces.Any())
                return true;

            var success = true;
            foreach (var face in faces)
            {
                if (!faceLibrary.TryAddFace(face))
                    success = false;
            }

            return success;
        }

        public static void InitFaceLibrary(this ConcurrentDictionary<string, Face> faceLibrary, IEnumerable<Face> faces)
        {
            faceLibrary.Clear();
            faceLibrary.AddFaces(faces);
        }

        public static bool TryInitFaceLibrary(this ConcurrentDictionary<string, Face> faceLibrary,
            IEnumerable<Face> faces)
        {
            faceLibrary.Clear();
            return faceLibrary.TryAddFaces(faces);
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
    }
}