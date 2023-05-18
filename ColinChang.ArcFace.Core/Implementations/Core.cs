using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core.Utils;
using ColinChang.ArcFace.Core.Extensions;

namespace ColinChang.ArcFace.Core;

/// <summary>
/// 核心功能 人脸检测/特征提取/人脸比对
/// </summary>
public partial class ArcFace
{
    public async Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(string image)
    {
        await using var img = image.ToStream();
        return await DetectFaceAsync(img);
    }

    public async Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(Stream image) =>
        await ProcessImageAsync<AsfMultiFaceInfo, MultiFaceInfo>(image, FaceHelper.DetectFaceAsync);

    public async Task<OperationResult<MultiFaceInfo>> DetectFaceFromBase64StringAsync(string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
            return new OperationResult<MultiFaceInfo>(default);

        await using var img = new MemoryStream(Convert.FromBase64String(base64Image));
        return await DetectFaceAsync(img);
    }

    public async Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(string image, LivenessMode mode)
    {
        await using var img = image.ToStream();
        return await GetLivenessInfoAsync(img, mode);
    }

    public async Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(Stream image,
        LivenessMode mode)
    {
        if (mode == LivenessMode.RGB)
            return await ProcessImageAsync<AsfLivenessInfo, LivenessInfo>(image, FaceHelper.GetRgbLivenessInfoAsync,
                DetectionModeEnum.RGB);

        return await ProcessImageAsync<AsfLivenessInfo, LivenessInfo>(image, FaceHelper.GetIrLivenessInfoAsync,
            DetectionModeEnum.IR);
    }

    public async Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(string image)
    {
        await using var img = image.ToStream();
        return await ProcessImageAsync<IEnumerable<byte[]>, IEnumerable<byte[]>>(img,
            FaceHelper.ExtractFeatureAsync);
    }

    public async Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(Stream image) =>
        await ProcessImageAsync<IEnumerable<byte[]>, IEnumerable<byte[]>>(image,
            FaceHelper.ExtractFeatureAsync);

    public async Task<OperationResult<float>> CompareFaceFeatureAsync(byte[] feature1, byte[] feature2) =>
        await Task.Run(() =>
        {
            var engine = IntPtr.Zero;
            var featureA = IntPtr.Zero;
            var featureB = IntPtr.Zero;
            try
            {
                engine = GetEngine(DetectionModeEnum.Image);
                featureA = feature1.ToFaceFeature();
                featureB = feature2.ToFaceFeature();

                var similarity = 0f;
                var code = AsfHelper.ASFFaceFeatureCompare(engine, featureA, featureB, ref similarity);
                return new OperationResult<float>(similarity, code);
            }
            finally
            {
                RecycleEngine(engine, DetectionModeEnum.Image);
                featureA.DisposeFaceFeature();
                featureB.DisposeFaceFeature();
            }
        });
}