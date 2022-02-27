using System;

namespace ColinChang.ArcFace
{
    public class InvalidFaceFeatureException : Exception
    {
        private string _message = "invalid face feature";

        public InvalidFaceFeatureException(string message = "invalid face feature", Exception innerException = null) : base(message, innerException)
        {
        }

        public InvalidFaceFeatureException(Exception exception) : this(innerException: exception)
        {
        }
    }
}
