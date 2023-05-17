using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ColinChang.ArcFace.Abstraction.Models
{
    public class ImageInfo : IDisposable, IAsyncDisposable
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

        public void Dispose() =>
            Marshal.FreeHGlobal(ImgData);

        public ValueTask DisposeAsync()
        {
            Marshal.FreeHGlobal(ImgData);
            return ValueTask.CompletedTask;
        }
    }
}