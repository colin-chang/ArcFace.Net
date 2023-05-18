using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ColinChang.ArcFace.Abstraction.Models;

namespace ColinChang.ArcFace.Abstraction
{
    /// <summary>
    /// 虹软人脸SDK工具库
    /// </summary>
    public interface IArcFace : IDisposable
    {
        #region SDK信息 激活信息/版本信息
        
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

        #endregion
        
        #region 人脸属性 3D角度/年龄/性别
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
        Task<OperationResult<Face3DAngle>> GetFace3DAngleAsync(Stream image);

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
        Task<OperationResult<AgeInfo>> GetAgeAsync(Stream image);

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
        Task<OperationResult<GenderInfo>> GetGenderAsync(Stream image);

        #endregion
        
        #region 核心功能 人脸检测/特征提取/人脸比对
        
        /// <summary>
        /// 人脸检测
        /// </summary>
        Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(string image);

        /// <summary>
        /// 人脸检测
        /// </summary>
        Task<OperationResult<MultiFaceInfo>> DetectFaceAsync(Stream image);

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
        Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(string image, LivenessMode mode);

        /// <summary>
        /// 活体检测
        /// </summary>
        /// <param name="image"></param>
        /// <param name="mode">检测模式,支持RGB、IR活体</param>
        /// <returns></returns>
        Task<OperationResult<LivenessInfo>> GetLivenessInfoAsync(Stream image, LivenessMode mode);

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
        Task<OperationResult<IEnumerable<byte[]>>> ExtractFaceFeatureAsync(Stream image);

        /// <summary>
        /// 人脸特征比对，输出比对相似度
        /// </summary>
        /// <param name="feature1">人脸特征</param>
        /// <param name="feature2">人脸特征</param>
        /// <returns></returns>
        Task<OperationResult<float>> CompareFaceFeatureAsync(byte[] feature1, byte[] feature2);

        #endregion
        
        # region 人脸库管理 初始化/新增人脸/删除人脸/搜索人脸
        
        /// <summary>
        /// 初始化人脸库(约定文件名为FaceId)
        /// </summary>
        /// <param name="images">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns></returns>
        Task InitFaceLibraryAsync(IEnumerable<string> images);

        /// <summary>
        /// 尝试初始化人脸库(约定文件名为FaceId)
        /// </summary>
        /// <param name="images">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns>(是否全部成功,成功记录数)</returns>
        Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<string> images);

        /// <summary>
        /// 初始化人脸库
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        Task InitFaceLibraryAsync(IEnumerable<Face> faces);

        /// <summary>
        /// 尝试初始化人脸库
        /// </summary>
        /// <param name="faces"></param>
        /// <returns>(是否全部成功,成功记录数)</returns>
        Task<(bool Success, int SuccessCount)> TryInitFaceLibraryAsync(IEnumerable<Face> faces);

        /// <summary>
        /// 人脸库新增人脸(约定文件名为FaceId)
        /// </summary>
        /// <param name="images">人脸图片。多人脸图自动选比例最大的人脸</param>
        Task AddFaceAsync(params string[] images);

        /// <summary>
        /// 尝试在人脸库中新增人脸(约定文件名为FaceId)
        /// </summary>
        /// <param name="images">人脸图片。多人脸图自动选比例最大的人脸</param>
        /// <returns>(是否全部新增成功,新增成功人脸数量)</returns>
        Task<(bool Success, int SuccessCount)> TryAddFaceAsync(params string[] images);

        /// <summary>
        /// 人脸库新增人脸
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        Task AddFaceAsync(params Face[] faces);

        /// <summary>
        /// 尝试在人脸库中新增人脸
        /// </summary>
        /// <param name="faces"></param>
        /// <returns>(是否全部新增成功,新增成功人脸数量)</returns>
        Task<(bool Success, int SuccessCount)> TryAddFaceAsync(params Face[] faces);

        /// <summary>
        /// 人脸库删除人脸
        /// </summary>
        /// <param name="faceIds"></param>
        /// <returns>成功删除人脸数量</returns>
        Task<int> RemoveFaceAsync(params string[] faceIds);

        /// <summary>
        /// 尝试人脸库删除人脸
        /// </summary>
        /// <param name="faceIds"></param>
        /// <returns>(人脸是否全部删除成功,成功删除人脸数)</returns>
        Task<(bool Success, int SuccessCount)> TryRemoveFaceAsync(params string[] faceIds);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image">头像路径</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(string image, Predicate<Face> predicate = null);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image">头像路径</param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(string image, float minSimilarity,
            Predicate<Face> predicate = null);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image">头像</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(Stream image, Predicate<Face> predicate = null);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="image">头像</param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(Stream image, float minSimilarity,
            Predicate<Face> predicate = null);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="feature">人脸特征</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature, Predicate<Face> predicate = null);

        /// <summary>
        /// 人脸库中搜索人脸
        /// </summary>
        /// <param name="feature">人脸特征</param>
        /// <param name="minSimilarity">最小人脸相似度</param>
        /// <param name="predicate">人脸筛选条件</param>
        /// <returns>搜索结果</returns>
        Task<OperationResult<Recognitions>> SearchFaceAsync(byte[] feature, float minSimilarity,
            Predicate<Face> predicate = null);
        
        #endregion
    }
}