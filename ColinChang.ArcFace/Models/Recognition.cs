using System.Collections.Generic;
using System.Linq;

namespace ColinChang.ArcFace.Models
{
    public class Recognitions
    {
        public IEnumerable<Recognition> RecognitionCollection { get; set; }

        public Recognition Recognition => RecognitionCollection.MaxBy(r => r.Similarity);

        public Recognitions(IEnumerable<Recognition> recognitionCollection) =>
            RecognitionCollection = recognitionCollection;
    }

    public class Recognition
    {
        /// <summary>
        /// 人脸ID
        /// </summary>
        public string FaceId { get; set; }

        /// <summary>
        /// 相似度
        /// </summary>
        public float Similarity { get; set; }

        public Recognition(string faceId, float similarity)
        {
            FaceId = faceId;
            Similarity = similarity;
        }
    }
}