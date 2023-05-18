using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Extensions;
using ColinChang.ArcFace.Abstraction.Models;
using ColinChang.ArcFace.Core.Utils;

namespace ColinChang.ArcFace.Core;

/// <summary>
/// 人脸属性 3D角度/年龄/性别
/// </summary>
public partial class ArcFace
{
    public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(string image)
    {
        await using var img = image.ToStream();
        return await GetFace3DAngleAsync(img);
    }

    public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(Stream image) =>
        await ProcessImageAsync<AsfFace3DAngle, Face3DAngle>(image, FaceHelper.GetFace3DAngleAsync);

    public async Task<OperationResult<AgeInfo>> GetAgeAsync(string image)
    {
        await using var img = image.ToStream();
        return await GetAgeAsync(img);
    }

    public async Task<OperationResult<AgeInfo>> GetAgeAsync(Stream image) =>
        await ProcessImageAsync<AsfAgeInfo, AgeInfo>(image, FaceHelper.GetAgeAsync);

    public async Task<OperationResult<GenderInfo>> GetGenderAsync(string image)
    {
        await using var img = image.ToStream();
        return await GetGenderAsync(img);
    }

    public async Task<OperationResult<GenderInfo>> GetGenderAsync(Stream image) =>
        await ProcessImageAsync<AsfGenderInfo, GenderInfo>(image, FaceHelper.GetGenderAsync);
}