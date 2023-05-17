using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ColinChang.ArcFace.Abstraction.Models;


namespace ColinChang.ArcFace.Extensions
{
    public static class ArcFaceExtension
    {
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
    }
}