using System.ComponentModel.DataAnnotations;
using ColinChang.FaceRecognition.Models;

namespace ColinChang.FaceRecognition
{
    public class FaceRecognitionOptions
    {
        [Required(ErrorMessage = "arc face AppId is required")]
        public string AppId { get; set; }

        [Required(ErrorMessage = "arc face SdkKey is required")]
        public string SdkKey { get; set; }

        /// <summary>
        /// 人脸识别成功的最小相似度
        /// </summary>
        [Required(ErrorMessage = "min similarity is required")]
        public float MinSimilarity { get; set; }

        /// <summary>
        /// 单图片最大检测的人脸数 [1,50]
        /// </summary>
        public int MaxDetectFaceNum { get; set; } = 5;

        /// <summary>
        /// 引擎池单类引擎数上限(过大会增加内存开支)
        /// </summary>
        public int MaxSingleTypeEngineCount { get; set; } = 3;

        /// <summary>
        /// 图像模式 检测脸部的角度优先值
        /// </summary>
        public int ImageDetectFaceOrientPriority { get; set; } = AsfOrientPriority.ASF_OP_0_ONLY;

        /// <summary>
        /// 视频模式 检测脸部的角度优先值
        /// </summary>
        public int VideoDetectFaceOrientPriority { get; set; } = AsfOrientPriority.ASF_OP_0_HIGHER_EXT;

        /// <summary>
        /// 图像模式 可识别最小人脸比例（图片长边与人脸框长边的比值）[2-32]
        /// </summary>
        public int ImageDetectFaceScaleVal { get; set; } = 32;

        /// <summary>
        /// 视频模式 可识别最小人脸比例（图片长边与人脸框长边的比值）[2-32]
        /// </summary>
        public int VideoDetectFaceScaleVal { get; set; } = 16;
    }
}