﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ColinChang.ArcFace.Abstraction;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core;
using ColinChang.ArcFace.ImageSharp.Extensions;

//测试图片
await using var test = File.OpenRead("Images/test.jpg");
await using var zys = File.OpenRead("Images/zys.jpg");
await using var xy = File.OpenRead("Images/xy.jpg");
await using var xy1 = File.OpenRead("Images/xy1.jpg");


var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var services = new ServiceCollection().AddArcFace(configuration.GetSection(nameof(ArcFaceOptions)))
    .BuildServiceProvider();
var arcFace = services.GetRequiredService<IArcFace>();


//获取激活消息
var info = await arcFace.GetActiveFileInfoAsync();
// 获取SDK信息
var version = await arcFace.GetSdkVersionAsync();
// 人脸检测
var faces = await arcFace.DetectFaceAsync(test);
// 获取年龄
var ages = await arcFace.GetAgeAsync(test);
// 获取性别
var genders = await arcFace.GetGenderAsync(test);
// 获取3D角度
var face3DAngleInfo = await arcFace.GetFace3DAngleAsync(test);

// 提取人脸特征
var features0 = await arcFace.ExtractFaceFeatureAsync(zys);
var features1 = await arcFace.ExtractFaceFeatureAsync(xy);

// 人脸比对
var result = await arcFace.CompareFaceFeatureAsync(features0.Data.Single(), features1.Data.Single());

// 活体检测实际应取视频帧图，此处仅作演示
//RGB活体检测
var livenessRgb = await arcFace.GetLivenessInfoAsync(zys, LivenessMode.RGB);
//IR活体检测
var livenessIr = await arcFace.GetLivenessInfoAsync(zys, LivenessMode.IR);


// 初始化人脸库
await arcFace.InitFaceLibraryAsync(new[] { "Images/xy.jpg" });
await arcFace.InitFaceLibraryAsync(new[] { "Images/xy1.jpg" }, "my"); //初始化多人脸库
// 搜索人脸库
var res = await arcFace.SearchFaceAsync(zys);
res = await arcFace.SearchFaceAsync(zys, libraryKey: "my"); //指定人脸库检索
if (res.Code == 0 && res.Data.RecognitionCollection.Any())
{
    var recognition = res.Data.Recognition;
    Console.WriteLine("FaceId:{0}\tSimilarity:{1}", recognition.FaceId, recognition.Similarity);
}


Console.ReadKey();