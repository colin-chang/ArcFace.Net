# FaceRecognition
A face recognition utility based on ArcSoft SDK 1.x for Windows platform.It can support multiple faces comparison.

Fill your AppId,FRkey and FDKey in appsettings.json under [sample project](https://github.com/colin-chang/FaceRecognition/tree/master/ColinChang.FaceRecognition.Sample),before you run it.you can get the key from [Arcsoft](https://ai.arcsoft.com.cn/product/arcface.html) freely.

This package is a wrapper of Arcsoft FaceRecognition SDK in C/C++.you can use it on .NET.It's developed on under .Net Standard 2.0,but you can use it on Windows platform,because of the limitation of Arcsoft SDK.

The Arcsoft SDK has different versions for x86 and x64.The `libarcsoft_fsdk_face_detection.dll` and `libarcsoft_fsdk_face_recognition.dll` under [FaceRecognition](https://github.com/colin-chang/FaceRecognition/tree/master/ColinChang.FaceRecognition) are x64 version by default.You can switch it to x86 version by 2 steps.
1. copy dlls under x86 directory to FaceRecognition project and set their "Copy to output directory" to "Copy always"
2. Set `Project target` to `x86`
