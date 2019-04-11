using System;
using System.Runtime.InteropServices;

namespace ColinChang.FaceRecognition.AFD
{
    public class AFDFunction
    {
        /*
         * 定义初始化FD SDK引擎的方法
         *
         */
        [DllImport("libarcsoft_fsdk_face_detection.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_InitialFaceEngine(string appId, string sdkKey, IntPtr pMem, int lMemSize, ref IntPtr pEngine, int iOrientPriority, int nScale, int nMaxFaceNum);

        
        /**
        * 获取人脸检测 SDK 版本信息
        *
        */
        [DllImport("libarcsoft_fsdk_face_detection.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AFD_FSDK_GetVersion(IntPtr pEngine);

        /**
        * 根据输入的图像检测出人脸位置，一般用于静态图像检测
        *
        */
        [DllImport("libarcsoft_fsdk_face_detection.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_StillImageFaceDetection(IntPtr pEngine, IntPtr pImgData, ref IntPtr pFaceRes);


        [DllImport("libarcsoft_fsdk_face_detection.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_UninitialFaceEngine(IntPtr pEngine);
    }
}
