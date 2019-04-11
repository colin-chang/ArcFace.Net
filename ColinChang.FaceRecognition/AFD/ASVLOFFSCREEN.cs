using System.Runtime.InteropServices;

namespace ColinChang.FaceRecognition.AFD
{
    public struct ASVLOFFSCREEN
    {
        public int u32PixelArrayFormat;

        public int i32Width;

        public int i32Height;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.SysUInt)]
        public System.IntPtr[] ppu8Plane;

       
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I4)]
        public int[] pi32Pitch;

    }
}
