﻿namespace ColinChang.ArcFace.Abstraction.Models
{
    /// <summary>
    /// 检测模式
    /// </summary>
    public struct AsfDetectionMode
    {
        /// <summary>
        /// Video模式，一般用于多帧连续检测
        /// </summary>
        public const uint ASF_DETECT_MODE_VIDEO = 0x00000000;

        /// <summary>
        /// Image模式，一般用于静态图的单次检测
        /// </summary>
        public const uint ASF_DETECT_MODE_IMAGE = 0xFFFFFFFF;
    }

    public enum DetectionModeEnum
    {
        Image,
        Video,
        RGB,
        IR
    }
}