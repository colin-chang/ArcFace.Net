using System;
using System.Runtime.InteropServices;

namespace ColinChang.ArcFace.Models
{
    public struct AsfMultiFaceInfo : ICast<MultiFaceInfo>
    {
        public IntPtr FaceRects { get; set; }

        public IntPtr FaceOrients { get; set; }

        public int FaceNum { get; set; }

        public IntPtr FaceId { get; set; }

        public MultiFaceInfo Cast()
        {
            var faces = new SingleFaceInfo[FaceNum];
            var rects = new Rect[FaceNum];
            var rectSize = Marshal.SizeOf<Rect>();
            var orients = new int[FaceNum];
            var intSize = Marshal.SizeOf<int>();
            for (var i = 0; i < FaceNum; i++)
            {
                var pointer = FaceRects + i * rectSize;
                rects[i] = Marshal.PtrToStructure<Rect>(pointer);
                orients[i] = Marshal.ReadInt32(FaceOrients, i * intSize);
                faces[i] = new SingleFaceInfo(rects[i], orients[i]);
            }

            return new MultiFaceInfo(rects, orients, FaceNum, FaceId, faces);
        }
    }

    /// <summary>
    /// 单人脸检测结构体
    /// </summary>
    public struct SingleFaceInfo
    {
        /// <summary>
        /// 人脸坐标Rect结果
        /// </summary>
        public Rect FaceRect { get; set; }

        /// <summary>
        /// 人脸角度
        /// </summary>
        public int FaceOrient { get; set; }

        public SingleFaceInfo(Rect faceRect, int faceOrient)
        {
            FaceRect = faceRect;
            FaceOrient = faceOrient;
        }
    }

    /// <summary>
    /// 多人脸检测结构体
    /// </summary>
    public struct MultiFaceInfo
    {
        /// <summary>
        /// 人脸Rect结果集
        /// </summary>
        public Rect[] FaceRects { get; set; }

        /// <summary>
        /// 人脸角度结果集，与faceRects一一对应  对应ASF_OrientCode(不准确)
        /// </summary>
        public int[] FaceOrients { get; set; }

        /// <summary>
        /// 结果集大小
        /// </summary>
        public int FaceNum { get; set; }

        /// <summary>
        /// face ID，IMAGE模式下不返回FaceID
        /// </summary>
        public IntPtr FaceId { get; set; }

        public SingleFaceInfo[] Faces { get; set; }

        public MultiFaceInfo(Rect[] faceRects, int[] faceOrients, int faceNum, IntPtr faceId, SingleFaceInfo[] faces)
        {
            FaceRects = faceRects;
            FaceOrients = faceOrients;
            FaceNum = faceNum;
            FaceId = faceId;
            Faces = faces;
        }
    }
}