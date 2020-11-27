using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ColinChang.FaceRecognition.Models;

namespace ColinChang.FaceRecognition.Utils
{
    public static class FaceUtil
    {
        /// <summary>
        /// 人脸检测
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="image">图像</param>
        /// <returns></returns>
        public static async Task<AsfMultiFaceInfo> DetectFaceAsync(IntPtr engine, Image image)
        {
            var imageInfo = ImageUtil.ReadBmp(image);
            var multiFaceInfo = await DetectFaceAsync(engine, imageInfo);
            Marshal.FreeHGlobal(imageInfo.ImgData);
            return multiFaceInfo;
        }

        /// <summary>
        /// 人脸检测(PS:检测RGB图像的人脸时，必须保证图像的宽度能被4整除，否则会失败)
        /// </summary>
        /// <param name="engine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <returns>人脸检测结果</returns>
        private static async Task<AsfMultiFaceInfo> DetectFaceAsync(IntPtr engine, ImageInfo imageInfo)
        {
            return await Task.Run(() =>
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
                var code = ASFFunctions.ASFDetectFaces(engine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                    imageInfo.ImgData, pointer);
                if (code != 0)
                {
                    Marshal.FreeHGlobal(pointer);
                    throw new Exception($"failed to detect face. error code {code}");
                }

                var multiFaceInfo = Marshal.PtrToStructure<AsfMultiFaceInfo>(pointer);
                Marshal.FreeHGlobal(pointer);
                return multiFaceInfo;
            });
        }


        /// <summary>
        /// 提取人脸特征
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="image">图像</param>
        /// <returns>保存人脸特征结构体指针</returns>
        public static async Task<IntPtr> ExtractFeatureAsync(IntPtr pEngine, Image image)
        {
            if (image.Width > 1536 || image.Height > 1536)
                image = ImageUtil.ScaleImage(image, 1536, 1536);
            else
                image = ImageUtil.ScaleImage(image, image.Width, image.Height);

            if (image == null)
            {
                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            var imageInfo = ImageUtil.ReadBmp(image);
            if (imageInfo == null)
            {
                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            var multiFaceInfo = await DetectFaceAsync(pEngine, imageInfo);
            var pFaceModel = ExtractFeature(pEngine, imageInfo, multiFaceInfo);
            Marshal.FreeHGlobal(imageInfo.ImgData);
            return pFaceModel;
        }

        /// <summary>
        /// 提取人脸特征
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <param name="singleFaceInfo"></param>
        /// <returns>保存人脸特征结构体指针</returns>
        private static IntPtr ExtractFeature(IntPtr pEngine, ImageInfo imageInfo, AsfMultiFaceInfo multiFaceInfo)
        {
            var singleFaceInfo = new ASF_SingleFaceInfo();
            if (multiFaceInfo.FaceNum <= 0)
            {
                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            singleFaceInfo.faceRect = Marshal.PtrToStructure<Rect>(multiFaceInfo.FaceRects);
            singleFaceInfo.faceOrient = Marshal.PtrToStructure<int>(multiFaceInfo.FaceOrients);
            var pSingleFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_SingleFaceInfo>());
            Marshal.StructureToPtr(singleFaceInfo, pSingleFaceInfo, false);

            var pFaceFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
            var retCode = ASFFunctions.ASFFaceFeatureExtract(pEngine, imageInfo.Width, imageInfo.Height,
                imageInfo.Format, imageInfo.ImgData, pSingleFaceInfo, pFaceFeature);
            Console.WriteLine("FR Extract Feature result:" + retCode);

            if (retCode != 0)
            {
                //释放指针
                Marshal.FreeHGlobal(pSingleFaceInfo);
                Marshal.FreeHGlobal(pFaceFeature);
                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            //人脸特征feature过滤
            var faceFeature = Marshal.PtrToStructure<ASF_FaceFeature>(pFaceFeature);
            var feature = new byte[faceFeature.featureSize];
            Marshal.Copy(faceFeature.feature, feature, 0, faceFeature.featureSize);

            var localFeature = new ASF_FaceFeature {feature = Marshal.AllocHGlobal(feature.Length)};
            Marshal.Copy(feature, 0, localFeature.feature, feature.Length);
            localFeature.featureSize = feature.Length;
            var pLocalFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
            Marshal.StructureToPtr(localFeature, pLocalFeature, false);

            //释放指针
            Marshal.FreeHGlobal(pSingleFaceInfo);
            Marshal.FreeHGlobal(pFaceFeature);

            return pLocalFeature;
        }

        /// <summary>
        /// 提取单人脸特征
        /// </summary>
        /// <param name="pEngine">人脸识别引擎</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>单人脸特征</returns>
        public static IntPtr ExtractFeature(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            var imageInfo = ImageUtil.ReadBmp(image);
            if (imageInfo == null)
            {
                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            var pSingleFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_SingleFaceInfo>());
            Marshal.StructureToPtr(singleFaceInfo, pSingleFaceInfo, false);

            var pFaceFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
            var retCode = -1;
            try
            {
                retCode = ASFFunctions.ASFFaceFeatureExtract(pEngine, imageInfo.Width, imageInfo.Height,
                    imageInfo.Format, imageInfo.ImgData, pSingleFaceInfo, pFaceFeature);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("FR Extract Feature result:" + retCode);

            if (retCode != 0)
            {
                //释放指针
                Marshal.FreeHGlobal(pSingleFaceInfo);
                Marshal.FreeHGlobal(pFaceFeature);
                Marshal.FreeHGlobal(imageInfo.ImgData);

                var emptyFeature = new ASF_FaceFeature();
                var pEmptyFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
                Marshal.StructureToPtr(emptyFeature, pEmptyFeature, false);
                return pEmptyFeature;
            }

            //人脸特征feature过滤
            var faceFeature = Marshal.PtrToStructure<ASF_FaceFeature>(pFaceFeature);
            var feature = new byte[faceFeature.featureSize];
            Marshal.Copy(faceFeature.feature, feature, 0, faceFeature.featureSize);

            var localFeature = new ASF_FaceFeature();
            localFeature.feature = Marshal.AllocHGlobal(feature.Length);
            Marshal.Copy(feature, 0, localFeature.feature, feature.Length);
            localFeature.featureSize = feature.Length;
            var pLocalFeature = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_FaceFeature>());
            Marshal.StructureToPtr(localFeature, pLocalFeature, false);

            //释放指针
            Marshal.FreeHGlobal(pSingleFaceInfo);
            Marshal.FreeHGlobal(pFaceFeature);
            Marshal.FreeHGlobal(imageInfo.ImgData);

            return pLocalFeature;
        }


        /// <summary>
        /// 年龄检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>年龄检测结构体</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, ImageInfo imageInfo, AsfMultiFaceInfo multiFaceInfo,
            out int retCode)
        {
            retCode = -1;
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                return new ASF_AgeInfo();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_AGE);
            if (retCode == 0)
            {
                //获取年龄信息
                var pAgeInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_AgeInfo>());
                retCode = ASFFunctions.ASFGetAge(pEngine, pAgeInfo);
                Console.WriteLine("Get Age Result:" + retCode);
                var ageInfo = Marshal.PtrToStructure<ASF_AgeInfo>(pAgeInfo);

                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                Marshal.FreeHGlobal(pAgeInfo);
                return ageInfo;
            }
            else
            {
                return new ASF_AgeInfo();
            }
        }


        /// <summary>
        /// 单人脸年龄检测
        /// </summary>
        /// <param name="pEngine">人脸识别引擎</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>年龄检测结果</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            var imageInfo = ImageUtil.ReadBmp(image);
            if (imageInfo == null)
            {
                return new ASF_AgeInfo();
            }

            var multiFaceInfo = new AsfMultiFaceInfo {FaceRects = Marshal.AllocHGlobal(Marshal.SizeOf<Rect>())};
            Marshal.StructureToPtr<Rect>(singleFaceInfo.faceRect, multiFaceInfo.FaceRects, false);
            multiFaceInfo.FaceOrients = Marshal.AllocHGlobal(Marshal.SizeOf<int>());
            Marshal.StructureToPtr<int>(singleFaceInfo.faceOrient, multiFaceInfo.FaceOrients, false);
            multiFaceInfo.FaceNum = 1;
            var ageInfo = AgeEstimation(pEngine, imageInfo, multiFaceInfo);
            Marshal.FreeHGlobal(imageInfo.ImgData);
            return ageInfo;
        }

        /// <summary>
        /// 性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存性别检测结果结构体</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, ImageInfo imageInfo,
            AsfMultiFaceInfo multiFaceInfo, out int retCode)
        {
            retCode = -1;
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                return new ASF_GenderInfo();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_GENDER);
            if (retCode == 0)
            {
                //获取性别信息
                var pGenderInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_GenderInfo>());
                retCode = ASFFunctions.ASFGetGender(pEngine, pGenderInfo);
                Console.WriteLine("Get Gender Result:" + retCode);
                var genderInfo = Marshal.PtrToStructure<ASF_GenderInfo>(pGenderInfo);

                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                Marshal.FreeHGlobal(pGenderInfo);

                return genderInfo;
            }
            else
            {
                return new ASF_GenderInfo();
            }
        }

        /// <summary>
        /// 人脸3D角度检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存人脸3D角度检测结果结构体</returns>
        public static ASF_Face3DAngle Face3DAngleDetection(IntPtr pEngine, ImageInfo imageInfo,
            AsfMultiFaceInfo multiFaceInfo, out int retCode)
        {
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                retCode = -1;
                return new ASF_Face3DAngle();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_FACE3DANGLE);
            if (retCode == 0)
            {
                //获取人脸3D角度
                var pFace3DAngleInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_Face3DAngle>());
                retCode = ASFFunctions.ASFGetFace3DAngle(pEngine, pFace3DAngleInfo);
                Console.WriteLine("Get Face3D Angle Result:" + retCode);
                var face3DAngle = Marshal.PtrToStructure<ASF_Face3DAngle>(pFace3DAngleInfo);

                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                Marshal.FreeHGlobal(pFace3DAngleInfo);

                return face3DAngle;
            }
            else
            {
                return new ASF_Face3DAngle();
            }
        }

        /// <summary>
        /// RGB活体检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">活体检测结果</param>
        /// <returns>保存活体检测结果结构体</returns>
        public static ASF_LivenessInfo LivenessInfo_RGB(IntPtr pEngine, ImageInfo imageInfo,
            AsfMultiFaceInfo multiFaceInfo, out int retCode)
        {
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                retCode = -1;
                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                return new ASF_LivenessInfo();
            }

            try
            {
                //人脸信息处理
                retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                    imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_LIVENESS);
                if (retCode == 0)
                {
                    //获取活体检测结果
                    var pLivenessInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_LivenessInfo>());
                    retCode = ASFFunctions.ASFGetLivenessScore(pEngine, pLivenessInfo);
                    Console.WriteLine("Get Liveness Result:" + retCode);
                    var livenessInfo = Marshal.PtrToStructure<ASF_LivenessInfo>(pLivenessInfo);

                    //释放内存
                    Marshal.FreeHGlobal(pMultiFaceInfo);
                    Marshal.FreeHGlobal(pLivenessInfo);
                    return livenessInfo;
                }
                else
                {
                    //释放内存
                    Marshal.FreeHGlobal(pMultiFaceInfo);
                    return new ASF_LivenessInfo();
                }
            }
            catch
            {
                retCode = -1;
                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                return new ASF_LivenessInfo();
            }
        }

        /// <summary>
        /// 红外活体检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">活体检测结果</param>
        /// <returns>保存活体检测结果结构体</returns>
        public static ASF_LivenessInfo LivenessInfo_IR(IntPtr pEngine, ImageInfo imageInfo,
            AsfMultiFaceInfo multiFaceInfo, out int retCode)
        {
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                retCode = -1;
                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                return new ASF_LivenessInfo();
            }

            try
            {
                //人脸信息处理
                retCode = ASFFunctions.ASFProcess_IR(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                    imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_IR_LIVENESS);
                if (retCode == 0)
                {
                    //获取活体检测结果
                    var pLivenessInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_LivenessInfo>());
                    retCode = ASFFunctions.ASFGetLivenessScore_IR(pEngine, pLivenessInfo);
                    Console.WriteLine("Get Liveness Result:" + retCode);
                    var livenessInfo = Marshal.PtrToStructure<ASF_LivenessInfo>(pLivenessInfo);

                    //释放内存
                    Marshal.FreeHGlobal(pMultiFaceInfo);
                    Marshal.FreeHGlobal(pLivenessInfo);
                    return livenessInfo;
                }
                else
                {
                    //释放内存
                    Marshal.FreeHGlobal(pMultiFaceInfo);
                    return new ASF_LivenessInfo();
                }
            }
            catch
            {
                retCode = -1;
                //释放内存
                Marshal.FreeHGlobal(pMultiFaceInfo);
                return new ASF_LivenessInfo();
            }
        }

        /// <summary>
        /// 性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存性别估计结果结构体</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, ImageInfo imageInfo,
            AsfMultiFaceInfo multiFaceInfo)
        {
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                return new ASF_GenderInfo();
            }

            //人脸信息处理
            var retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_GENDER);

            //获取性别信息
            var pGenderInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_GenderInfo>());
            retCode = ASFFunctions.ASFGetGender(pEngine, pGenderInfo);
            Console.WriteLine("Get Gender Result:" + retCode);
            var genderInfo = Marshal.PtrToStructure<ASF_GenderInfo>(pGenderInfo);

            //释放内存
            Marshal.FreeHGlobal(pMultiFaceInfo);
            Marshal.FreeHGlobal(pGenderInfo);

            return genderInfo;
        }


        /// <summary>
        /// 单人脸性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>性别估计结果</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            var imageInfo = ImageUtil.ReadBmp(image);
            if (imageInfo == null)
            {
                return new ASF_GenderInfo();
            }

            var multiFaceInfo = new AsfMultiFaceInfo {FaceRects = Marshal.AllocHGlobal(Marshal.SizeOf<Rect>())};
            Marshal.StructureToPtr<Rect>(singleFaceInfo.faceRect, multiFaceInfo.FaceRects, false);
            multiFaceInfo.FaceOrients = Marshal.AllocHGlobal(Marshal.SizeOf<int>());
            Marshal.StructureToPtr<int>(singleFaceInfo.faceOrient, multiFaceInfo.FaceOrients, false);
            multiFaceInfo.FaceNum = 1;
            var genderInfo = GenderEstimation(pEngine, imageInfo, multiFaceInfo);
            Marshal.FreeHGlobal(imageInfo.ImgData);
            return genderInfo;
        }

        /// <summary>
        /// 年龄检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>年龄检测结构体</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, ImageInfo imageInfo, AsfMultiFaceInfo multiFaceInfo)
        {
            var pMultiFaceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<AsfMultiFaceInfo>());
            Marshal.StructureToPtr(multiFaceInfo, pMultiFaceInfo, false);

            if (multiFaceInfo.FaceNum == 0)
            {
                return new ASF_AgeInfo();
            }

            //人脸信息处理
            var retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.Width, imageInfo.Height, imageInfo.Format,
                imageInfo.ImgData, pMultiFaceInfo, FaceEngineMask.ASF_AGE);

            //获取年龄信息
            var pAgeInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_AgeInfo>());
            retCode = ASFFunctions.ASFGetAge(pEngine, pAgeInfo);
            Console.WriteLine("Get Age Result:" + retCode);
            var ageInfo = Marshal.PtrToStructure<ASF_AgeInfo>(pAgeInfo);

            //释放内存
            Marshal.FreeHGlobal(pMultiFaceInfo);
            Marshal.FreeHGlobal(pAgeInfo);

            return ageInfo;
        }

        /// <summary>
        /// 获取多个人脸检测结果中面积最大的人脸
        /// </summary>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>面积最大的人脸信息</returns>
        public static ASF_SingleFaceInfo GetMaxFace(AsfMultiFaceInfo multiFaceInfo)
        {
            var singleFaceInfo = new ASF_SingleFaceInfo {faceRect = new Rect(), faceOrient = 1};

            var maxArea = 0;
            var index = -1;
            for (var i = 0; i < multiFaceInfo.FaceNum; i++)
            {
                try
                {
                    var rect = Marshal.PtrToStructure<Rect>(multiFaceInfo.FaceRects + Marshal.SizeOf<Rect>() * i);
                    var area = (rect.Right - rect.Left) * (rect.Bottom - rect.Top);
                    if (maxArea > area) continue;
                    maxArea = area;
                    index = i;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (index == -1) return singleFaceInfo;
            singleFaceInfo.faceRect =
                Marshal.PtrToStructure<Rect>(multiFaceInfo.FaceRects + Marshal.SizeOf<Rect>() * index);
            singleFaceInfo.faceOrient =
                Marshal.PtrToStructure<int>(multiFaceInfo.FaceOrients + Marshal.SizeOf<int>() * index);

            return singleFaceInfo;
        }
    }
}