# FaceRecognition
This is a face recognition utility based on ArcSoft SDK 1.x for Windows platform.It can support multiple faces comparison.

## Nuget
* [ColinChang.FaceRecognition](https://www.nuget.org/packages/ColinChang.FaceRecognition/) for Windows x64
* [ColinChang.FaceRecognition.x86](https://www.nuget.org/packages/ColinChang.FaceRecognition.x86/) for Windows x86


## How to Run

Fill your `AppId`,`FRkey` and `FDKey` in appsettings.json under [sample project](https://github.com/colin-chang/FaceRecognition/tree/master/ColinChang.FaceRecognition.Sample),before running it.you can get the key from [Arcsoft](https://ai.arcsoft.com.cn/product/arcface.html) freely.

## Limitation
This package is a wrapper of Arcsoft FaceRecognition SDK in C/C++.you can use it on .NET platform.It's developed on under .Net Standard 2.0,however **it can only be used on Windows platform**,because of the limitation of Arcsoft SDK.

## x86/x64
The Arcsoft SDK has different versions for x86 and x64.The `libarcsoft_fsdk_face_detection.dll` and `libarcsoft_fsdk_face_recognition.dll` under [FaceRecognition](https://github.com/colin-chang/FaceRecognition/tree/master/ColinChang.FaceRecognition) are x64 version by default.You can switch it to x86 version by 2 steps.

* copy dlls under [x86 directory](https://github.com/colin-chang/FaceRecognition/tree/master/ColinChang.FaceRecognition/Sdk/x86) to FaceRecognition project and set their "Copy to output directory" to "Copy always"
* Set `Project target` to `x86`


# Enviroment
## 1. Dependencies
```bash
# for Ubuntu 20.04
sudo apt install libgdiplus/focal
```

## Tips
× 人脸检测和特性提取目前进识别到的人脸角度不准确，目前识别到的角度均为0度。
* 视频模式人脸追踪未开发