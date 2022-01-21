using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ColinChang.ArcFace.Models;

namespace ColinChang.ArcFace
{
    /// <summary>
    /// 虹软人脸SDK工具库
    /// </summary>
    public interface IArcFace : IDisposable
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
        /// 获取3D角度信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(Image image);

        /// <summary>
        /// 获取年龄信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<AgeInfo>> GetAgeAsync(string image);
        
        /// <summary>
        /// 获取年龄信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<AgeInfo>> GetAgeAsync(Image image);

        /// <summary>
        /// 获取性别信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<GenderInfo>> GetGenderAsync(string image);
        
        /// <summary>
        /// 获取性别信息
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<GenderInfo>> GetGenderAsync(Image image);

        /// <summary>
        /// 人脸检测
        /// </summary>
        Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(string image);

        /// <summary>
        /// 人脸检测
        /// </summary>
        Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(Image image);
        
        /// <summary>
        /// 人脸检测
        /// </summary>
        /// <returns></returns>
        Task<OperationResult<MultiFaceInfo>> DetectFaceFromBase64StringAsync(string base64Image);
        
        /// <summary>
        /// 活体检测
        /// </summary>
        /// <param name="image"></param>
        /// <param name="mode">检测模式,支持RGB、IR活体</param>
        /// <returns></returns>
        Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(Image image, LivenessMode mode);

        /// <summary>
        /// 人脸特征提取
        /// </summary>
        /// <returns></returns>
        Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(string image);

        /// <summary>
        /// 人脸特征提取
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(Image image);
        
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
        /// 人脸库新增人脸(约定文件名为FaceId)
        /// </summary>
        /// <param name="image">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns>状态码，0表示成功，非0表示有错误，具体状态码含义参考文档</returns>
        Task<long> AddFaceAsync(string image);

        /// <summary>
        /// 人脸库新增人脸
        /// </summary>
        /// <param name="faceId">人脸ID</param>
        /// <param name="feature">人脸特征</param>
        /// <returns></returns>
        Task AddFaceAsync(string faceId, byte[] feature);

        /// <summary>
        /// 人脸库删除人脸
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
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image"></param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(string image,float minSimilarity);
        
        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image"></param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(Image image);
        
        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image"></param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(Image image,float minSimilarity);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="feature"></param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(byte[] feature);
        
        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognition>> SearchFaceAsync(byte[] feature,float minSimilarity);
    }
}