using System;
using System.Runtime.InteropServices;

namespace ColinChang.FaceRecognition.Models
{
    /// <summary>
    /// 多人脸检测结构体
    /// </summary>
    public struct AsfMultiFaceInfo
    {
        public IntPtr FaceRects { get; set; }

        public IntPtr FaceOrients { get; set; }

        public int FaceNum { get; set; }

        public IntPtr FaceId { get; set; }

        public MultiFaceInfo Cast()
        {
            var rects = new Rect[FaceNum];
            var rectSize = Marshal.SizeOf<Rect>();
            var orients = new int[FaceNum];
            var intSize = Marshal.SizeOf<int>();
            for (var i = 0; i < FaceNum; i++)
            {
                var pointer = FaceRects + i * rectSize;
                rects[i] = Marshal.PtrToStructure<Rect>(pointer);
                pointer = FaceOrients + i * intSize;
                orients[i] = Marshal.PtrToStructure<int>(pointer);
            }

            return new MultiFaceInfo(rects, orients, FaceNum, FaceId);
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

        public MultiFaceInfo(Rect[] faceRects, int[] faceOrients, int faceNum, IntPtr faceId)
        {
            FaceRects = faceRects;
            FaceOrients = faceOrients;
            FaceNum = faceNum;
            FaceId = faceId;
        }
    }
}