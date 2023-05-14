using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Models;
using ColinChang.ArcFace.Utils;

namespace ColinChang.ArcFace;

/// <summary>
/// 人脸属性 3D角度/年龄/性别
/// </summary>
public partial class ArcFace
{
    public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(string image) =>
        await ProcessImageAsync<AsfFace3DAngle, Face3DAngle>(image, FaceHelper.GetFace3DAngleAsync);

    public async Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(Stream image)
    {
        using var img = await image.ToImage();
        return await ProcessImageAsync<AsfFace3DAngle, Face3DAngle>(img, FaceHelper.GetFace3DAngleAsync);
    }

    public async Task<OperationResult<AgeInfo>> GetAgeAsync(string image) =>
        await ProcessImageAsync<AsfAgeInfo, AgeInfo>(image, FaceHelper.GetAgeAsync);

    public async Task<OperationResult<AgeInfo>> GetAgeAsync(Stream image)
    {
        using var img = await image.ToImage();
        return await ProcessImageAsync<AsfAgeInfo, AgeInfo>(img, FaceHelper.GetAgeAsync);
    }

    public async Task<OperationResult<GenderInfo>> GetGenderAsync(string image) =>
        await ProcessImageAsync<AsfGenderInfo, GenderInfo>(image, FaceHelper.GetGenderAsync);

    public async Task<OperationResult<GenderInfo>> GetGenderAsync(Stream image)
    {
        using var img = await image.ToImage();
        return await ProcessImageAsync<AsfGenderInfo, GenderInfo>(img, FaceHelper.GetGenderAsync);
    }
}