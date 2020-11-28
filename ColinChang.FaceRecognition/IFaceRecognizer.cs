using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColinChang.FaceRecognition.Models;

namespace ColinChang.FaceRecognition
{
    public interface IFaceRecognizer : IDisposable
    {
        /// <summary>
        /// 获取激活文件信息
        /// </summary>
        /// <returns></returns>
        Task<OperationResult<ActiveFileInfo>> GetActiveFileInfoAsync();

        /// <summary>
        /// 获取SDK版本信息
        /// </summary>
        /// <returns></returns>
        Task<VersionInfo> GetSdkVersionAsync();

        /// <summary>
        /// 获取3D角度信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(string image);

        /// <summary>
        /// 获取年龄信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<AgeInfo>> GetAgeAsync(string image);

        /// <summary>
        /// 获取性别信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<GenderInfo>> GetGenderAsync(string image);

        /// <summary>
        /// 人脸检测
        /// </summary>
        Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(string image);

        /// <summary>
        /// 人脸特征提取
        /// </summary>
        /// <returns></returns>
        Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(string image);

        /// <summary>
        /// 人脸特征比对，输出比对相似度
        /// </summary>
        /// <param name="feature1">人脸特征</param>
        /// <param name="feature2">人脸特征</param>
        /// <returns></returns>
        Task<OperationResult<float>> CompareFaceFeatureAsync(byte[] feature1, byte[] feature2);

        /// <summary>
        /// 初始化人脸库(约定文件名为FaceId)
        /// </summary>
        /// <param name="images">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns></returns>
        Task InitFaceLibraryAsync(IEnumerable<string> images);

        /// <summary>
        /// 初始化人脸库
        /// </summary>
        /// <param name="faceFeatures"></param>
        /// <returns></returns>
        Task InitFaceLibraryAsync(Dictionary<string, byte[]> faceFeatures);

        /// <summary>
        /// 新增人脸(约定文件名为FaceId)
        /// </summary>
        /// <param name="image">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns>状态码，0表示成功，非0表示有错误，具体状态码含义参考文档</returns>
        Task<long> AddFaceAsync(string image);

        /// <summary>
        /// 新增人脸
        /// </summary>
        /// <param name="faceId">人脸ID</param>
        /// <param name="feature">人脸特征</param>
        /// <returns></returns>
        Task AddFaceAsync(string faceId, byte[] feature);

        /// <summary>
        /// 删除人脸
        /// </summary>
        /// <param name="faceId"></param>
        /// <returns></returns>
        Task RemoveFaceAsync(string faceId);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image"></param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(string image);

        /// <summary>
        /// 在人脸库中检索人脸
        /// </summary>
        /// <param name="feature"></param>
        /// <returns>检索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(byte[] feature);
    }
}