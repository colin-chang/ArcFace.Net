using System.Runtime.InteropServices;

namespace ColinChang.FaceRecognition.AFR
{
    public class AFRFunction
    {
       /**
        *Init Engine 
        */
       [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_InitialEngine", CallingConvention = CallingConvention.Cdecl)]
       public static extern int AFR_FSDK_InitialEngine(string AppId, string SDKKey,  System.IntPtr pMem, int lMemSize, ref System.IntPtr phEngine);


       [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_ExtractFRFeature", CallingConvention = CallingConvention.Cdecl)]
       public static extern int AFR_FSDK_ExtractFRFeature(System.IntPtr hEngine,  System.IntPtr pInputImage,  System.IntPtr pFaceRes, System.IntPtr pFaceModels);


       [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_FacePairMatching", CallingConvention = CallingConvention.Cdecl)]
       public static extern int AFR_FSDK_FacePairMatching(System.IntPtr hEngine,  System.IntPtr reffeature,  System.IntPtr probefeature, ref float pfSimilScore);

       [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_UninitialEngine", CallingConvention = CallingConvention.Cdecl)]
       public static extern int AFR_FSDK_UninitialEngine(System.IntPtr hEngine);

       [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_GetVersion", CallingConvention = CallingConvention.Cdecl)]
       public static extern System.IntPtr AFR_FSDK_GetVersion(System.IntPtr hEngine);

   
   }
}
