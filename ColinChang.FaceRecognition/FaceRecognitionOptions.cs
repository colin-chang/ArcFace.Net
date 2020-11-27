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
        /// 单类引擎最大值
        /// </summary>
        public int MaxSingleTypeEngineCount { get; set; } = 3;

        /// <summary>
        /// Image模式下 检测脸部的角度优先值
        /// </summary>
        public int ImageDetectFaceOrientPriority { get; set; } = ASF_OrientPriority.ASF_OP_0_ONLY;

        /// <summary>
        /// Video模式下检测脸部的角度优先值
        /// </summary>
        public int VideoDetectFaceOrientPriority { get; set; } = ASF_OrientPriority.ASF_OP_0_HIGHER_EXT;

        /// <summary>
        /// 识别的最小人脸比例（图片长边与人脸框长边的比值）[2-32]
        /// </summary>
        public int ImageDetectFaceScaleVal { get; set; } = 32;

        public int VideoDetectFaceScaleVal { get; set; } = 16;

        /// <summary>
        /// 最大需要检测的人脸个数 [1,50]
        /// </summary>
        public int DetectFaceMaxNum { get; set; } = 5;
        
    }
}