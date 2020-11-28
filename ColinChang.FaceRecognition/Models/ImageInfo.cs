using System;

namespace ColinChang.FaceRecognition.Models
{
    public class ImageInfo
    {
        /// <summary>
        /// 图片的像素数据
        /// </summary>
        public IntPtr ImgData { get; set; }

        /// <summary>
        /// 图片像素宽
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图片像素高
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 图片格式
        /// </summary>
        public int Format { get; set; }
    }
}