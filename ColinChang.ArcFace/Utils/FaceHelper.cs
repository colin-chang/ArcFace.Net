using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ColinChang.ArcFace.Models;

namespace ColinChang.ArcFace.Utils
{
    public static class FaceHelper
    {
        /// <summary>
        /// 人脸检测(PS:检测RGB图像的人脸时，必须保证图像的宽度能被4整除，否则会失败)
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image">图像数据</param>
        /// <returns>人脸检测结果</returns>
        public static async Task<OperationResult<AsfMultiFaceInfo>> DetectFaceAsync(IntPtr engine, Image image) =>
            await Task.Run(() =>
            {
                ImageInfo imageInfo = null;
                var pointer = IntPtr.Zero;
                try
                {
                    imageInfo = ImageHelper.ReadBmp(image);
                    pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                    var code = AsfHelper.ASFDetectFaces(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                        imageInfo.ImgData, pointer);
                    if (code != 0)
                        return new OperationResult<AsfMultiFaceInfo>(code);
                    var multiFaceInfo = Marshal.PtrToStructure<AsfMultiFaceInfo>(pointer);
                    return new OperationResult<AsfMultiFaceInfo>(multiFaceInfo);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pointer != IntPtr.Zero)
                        Marshal.FreeHGlobal(pointer);
                }
            });

        /// <summary>
        /// 提取人脸特征
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image"></param>
        /// <returns>保存人脸特征结构体指针</returns>
        public static async Task<OperationResult<IEnumerable<byte[]>>>
            ExtractFeatureAsync(IntPtr engine, Image image) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pSingleFaceInfo = IntPtr.Zero;
                var pFaceFeature = IntPtr.Zero;
                try
                {
                    var asfFaces = await DetectFaceAsync(engine, image);
                    if (asfFaces.Code != 0)
                        return new OperationResult<IEnumerable<byte[]>>(asfFaces.Code);

                    var faces = asfFaces.Data.Cast();
                    if (faces.FaceNum <= 0)
                        return new OperationResult<IEnumerable<byte[]>>(null);

                    var features = new byte[faces.FaceNum][];
                    for (var i = 0; i < faces.FaceNum; i++)
                    {
                        var singleFaceInfo = faces.Faces[i];
                        pSingleFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<SingleFaceInfo>());
                        Marshal.StructureToPtr(singleFaceInfo, pSingleFaceInfo, false);

                        imageInfo = ImageHelper.ReadBmp(image);
                        pFaceFeature = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFaceFeature>());
                        var code = AsfHelper.ASFFaceFeatureExtract(engine, imageInfo.Width, imageInfo.Height,
                            imageInfo.Format, imageInfo.ImgData, pSingleFaceInfo, pFaceFeature);
                        if (code != 0)
                            return new OperationResult<IEnumerable<byte[]>>(code);

                        var faceFeature = Marshal.PtrToStructure<AsfFaceFeature>(pFaceFeature);
                        var feature = new byte[faceFeature.FeatureSize];
                        Marshal.Copy(faceFeature.Feature, feature, 0, faceFeature.FeatureSize);
                        features[i] = feature;
                    }

                    return new OperationResult<IEnumerable<byte[]>>(features);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pSingleFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pSingleFaceInfo);
                    if (pFaceFeature != IntPtr.Zero)
                        Marshal.FreeHGlobal(pFaceFeature);
                }
            });

        /// <summary>
        /// 提取最大人脸特征
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image"></param>
        /// <returns>保存人脸特征结构体指针</returns>
        public static async Task<OperationResult<IntPtr>> ExtractSingleFeatureAsync(IntPtr engine, Image image) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pSingleFaceInfo = IntPtr.Zero;
                var pFaceFeature = IntPtr.Zero;
                try
                {
                    var asfFaces = await DetectFaceAsync(engine, image);
                    if (asfFaces.Code != 0)
                        return new OperationResult<IntPtr>(asfFaces.Code);

                    var faces = asfFaces.Data.Cast();
                    if (faces.FaceNum <= 0)
                        return new OperationResult<IntPtr>(IntPtr.Zero);

                    var singleFaceInfo = await GetBiggestFaceAsync(faces);
                    pSingleFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<SingleFaceInfo>());
                    Marshal.StructureToPtr(singleFaceInfo, pSingleFaceInfo, false);

                    imageInfo = ImageHelper.ReadBmp(image);
                    pFaceFeature = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFaceFeature>());
                    var code = AsfHelper.ASFFaceFeatureExtract(engine, imageInfo.Width, imageInfo.Height,
                        imageInfo.Format, imageInfo.ImgData, pSingleFaceInfo, pFaceFeature);
                    if (code != 0)
                        return new OperationResult<IntPtr>(code);

                    /*使用同一个引擎时，每次特征提取后的临时内存存储地址相同，后面的特征提取会覆盖之前的结果。
                     如要保存每次提取的特征，需要拷贝保存到单独的内存
                     */
                    var faceFeature = Marshal.PtrToStructure<AsfFaceFeature>(pFaceFeature);
                    var feature = new byte[faceFeature.FeatureSize];
                    Marshal.Copy(faceFeature.Feature, feature, 0, faceFeature.FeatureSize);

                    var localFeature = new AsfFaceFeature {Feature = Marshal.AllocHGlobal(feature.Length)};
                    Marshal.Copy(feature, 0, localFeature.Feature, feature.Length);
                    localFeature.FeatureSize = feature.Length;
                    var pLocalFeature = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFaceFeature>());
                    Marshal.StructureToPtr(localFeature, pLocalFeature, false);
                    return new OperationResult<IntPtr>(pLocalFeature);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pSingleFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pSingleFaceInfo);
                    if (pFaceFeature != IntPtr.Zero)
                        Marshal.FreeHGlobal(pFaceFeature);
                }
            });

        /// <summary>
        /// 年龄检测
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image"></param>
        /// <returns>年龄检测结构体</returns>
        public static async Task<OperationResult<AsfAgeInfo>> GetAgeAsync(IntPtr engine, Image image) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pMultiFaceInfo = IntPtr.Zero;
                var pAgeInfo = IntPtr.Zero;
                try
                {
                    var faces = await DetectFaceAsync(engine, image);
                    if (faces.Code != 0)
                        return new OperationResult<AsfAgeInfo>(faces.Code);
                    if (faces.Data.FaceNum <= 0)
                        return new OperationResult<AsfAgeInfo>(new AsfAgeInfo());

                    imageInfo = ImageHelper.ReadBmp(image);
                    pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                    Marshal.StructureToPtr(faces.Data, pMultiFaceInfo, false);

                    //人脸信息处理
                    var code = AsfHelper.ASFProcess(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                        imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_AGE);
                    if (code != 0)
                        return new OperationResult<AsfAgeInfo>(code);

                    //获取年龄信息
                    pAgeInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfAgeInfo>());
                    code = AsfHelper.ASFGetAge(engine, pAgeInfo);
                    if (code != 0)
                        return new OperationResult<AsfAgeInfo>(code);

                    var ageInfo = Marshal.PtrToStructure<AsfAgeInfo>(pAgeInfo);
                    return new OperationResult<AsfAgeInfo>(ageInfo);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pMultiFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pMultiFaceInfo);
                    if (pAgeInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pAgeInfo);
                }
            });

        /// <summary>
        /// 性别检测
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image"></param>
        /// <returns>保存性别检测结果结构体</returns>
        public static async Task<OperationResult<AsfGenderInfo>> GetGenderAsync(IntPtr engine, Image image) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pMultiFaceInfo = IntPtr.Zero;
                var pGenderInfo = IntPtr.Zero;
                try
                {
                    var faces = await DetectFaceAsync(engine, image);
                    if (faces.Code != 0)
                        return new OperationResult<AsfGenderInfo>(faces.Code);
                    if (faces.Data.FaceNum <= 0)
                        return new OperationResult<AsfGenderInfo>(new AsfGenderInfo());

                    imageInfo = ImageHelper.ReadBmp(image);
                    pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                    Marshal.StructureToPtr(faces.Data, pMultiFaceInfo, false);

                    //人脸信息处理
                    var code = AsfHelper.ASFProcess(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                        imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_GENDER);
                    if (code != 0)
                        return new OperationResult<AsfGenderInfo>(code);

                    //获取性别信息
                    pGenderInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfGenderInfo>());
                    code = AsfHelper.ASFGetGender(engine, pGenderInfo);
                    if (code != 0)
                        return new OperationResult<AsfGenderInfo>(code);

                    var genderInfo = Marshal.PtrToStructure<AsfGenderInfo>(pGenderInfo);
                    return new OperationResult<AsfGenderInfo>(genderInfo);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pMultiFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pMultiFaceInfo);
                    if (pGenderInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pGenderInfo);
                }
            });

        /// <summary>
        /// 人脸3D角度检测
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image"></param>
        /// <returns>保存人脸3D角度检测结果结构体</returns>
        public static async Task<OperationResult<AsfFace3DAngle>> GetFace3DAngleAsync(IntPtr engine, Image image) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pMultiFaceInfo = IntPtr.Zero;
                var pFace3DAngleInfo = IntPtr.Zero;
                try
                {
                    var faces = await DetectFaceAsync(engine, image);
                    if (faces.Code != 0)
                        return new OperationResult<AsfFace3DAngle>(faces.Code);
                    if (faces.Data.FaceNum <= 0)
                        return new OperationResult<AsfFace3DAngle>(new AsfFace3DAngle());

                    imageInfo = ImageHelper.ReadBmp(image);
                    pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                    Marshal.StructureToPtr(faces.Data, pMultiFaceInfo, false);

                    //人脸信息处理
                    var code = AsfHelper.ASFProcess(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                        imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_FACE3DANGLE);
                    if (code != 0)
                        return new OperationResult<AsfFace3DAngle>(code);

                    //获取人脸3D角度
                    pFace3DAngleInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfFace3DAngle>());
                    code = AsfHelper.ASFGetFace3DAngle(engine, pFace3DAngleInfo);
                    if (code != 0)
                        return new OperationResult<AsfFace3DAngle>(code);

                    var face3DAngle = Marshal.PtrToStructure<AsfFace3DAngle>(pFace3DAngleInfo);
                    return new OperationResult<AsfFace3DAngle>(face3DAngle);
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pMultiFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pMultiFaceInfo);
                    if (pFace3DAngleInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pFace3DAngleInfo);
                }
            });


        /// <summary>
        /// RGB可见光活体检测
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Task<OperationResult<AsfLivenessInfo>> GetRgbLivenessInfoAsync(IntPtr engine, Image image)
            => GetLivenessInfoAsync(engine, image, LivenessMode.RGB);

        /// <summary>
        /// IR红外活体检测
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Task<OperationResult<AsfLivenessInfo>> GetIrLivenessInfoAsync(IntPtr engine, Image image)
            => GetLivenessInfoAsync(engine, image, LivenessMode.IR);


        /// <summary>
        /// 活体检测
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image">图像数据</param>
        /// <param name="mode"></param>
        /// <returns>保存活体检测结果结构体</returns>
        private static async Task<OperationResult<AsfLivenessInfo>> GetLivenessInfoAsync(IntPtr engine, Image image,
            LivenessMode mode) =>
            await Task.Run(async () =>
            {
                ImageInfo imageInfo = null;
                var pMultiFaceInfo = IntPtr.Zero;
                var pLivenessInfo = IntPtr.Zero;
                try
                {
                    var asfFaces = await DetectFaceAsync(engine, image);
                    if (asfFaces.Code != 0)
                        return new OperationResult<AsfLivenessInfo>(asfFaces.Code);

                    var faces = asfFaces.Data.Cast();
                    if (faces.FaceNum <= 0)
                        return new OperationResult<AsfLivenessInfo>(new AsfLivenessInfo());

                    pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                    Marshal.StructureToPtr(asfFaces.Data, pMultiFaceInfo, false);

                    //人脸信息处理
                    int code;
                    //获取活体检测结果
                    pLivenessInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfLivenessInfo>());
                    if (mode == LivenessMode.RGB)
                    {
                        imageInfo = ImageHelper.ReadBmp(image);
                        code = AsfHelper.ASFProcess(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                            imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_LIVENESS);
                        if (code != 0)
                            return new OperationResult<AsfLivenessInfo>(code);
                        code = AsfHelper.ASFGetLivenessScore(engine, pLivenessInfo);
                    }
                    else
                    {
                        imageInfo = ImageHelper.ReadBMP_IR(new Bitmap(image));
                        code = AsfHelper.ASFProcess_IR(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                            imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_IR_LIVENESS);
                        if (code != 0)
                            return new OperationResult<AsfLivenessInfo>(code);
                        code = AsfHelper.ASFGetLivenessScore_IR(engine, pLivenessInfo);
                    }

                    return code != 0
                        ? new OperationResult<AsfLivenessInfo>(code)
                        : new OperationResult<AsfLivenessInfo>(Marshal.PtrToStructure<AsfLivenessInfo>(pLivenessInfo));
                }
                finally
                {
                    if (imageInfo != null)
                        Marshal.FreeHGlobal(imageInfo.ImgData);
                    if (pMultiFaceInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pMultiFaceInfo);
                    if (pLivenessInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(pLivenessInfo);
                }
            });


        /// <summary>
        /// 获取多个人脸检测结果中面积最大的人脸
        /// </summary>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>面积最大的人脸信息</returns>
        private static async Task<SingleFaceInfo> GetBiggestFaceAsync(MultiFaceInfo multiFaceInfo) =>
            await Task.Run(() =>
            {
                var singleFaceInfo = new SingleFaceInfo(new Rect(), 1);
                if (multiFaceInfo.FaceNum <= 0)
                    return singleFaceInfo;

                var maxArea = 0;
                foreach (var face in multiFaceInfo.Faces)
                {
                    var area = (face.FaceRect.Right - face.FaceRect.Left) * (face.FaceRect.Bottom - face.FaceRect.Top);
                    if (area <= maxArea)
                        continue;

                    maxArea = area;
                    singleFaceInfo = face;
                }

                return singleFaceInfo;
            });
    }
}