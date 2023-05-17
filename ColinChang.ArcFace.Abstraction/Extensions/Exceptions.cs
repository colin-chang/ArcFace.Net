using System;

namespace ColinChang.ArcFace.Abstraction.Extensions
{
    public class InvalidFaceFeatureException : Exception
    {
        private string _message = "invalid face feature";

        public InvalidFaceFeatureException(string message = "invalid face feature", Exception innerException = null) :
            base(message, innerException)
        {
        }

        public InvalidFaceFeatureException(Exception exception) : this(innerException: exception)
        {
        }
    }

    public class NoFaceImageException : Exception
    {
        public string Filename { get; }

        public long Code { get; }

        public NoFaceImageException(long code, string filename = null) : base(
            $"{(string.IsNullOrWhiteSpace(filename) ? string.Empty : filename + " ")}failed to extract face feature. code:{code}")
        {
            Filename = filename;
            Code = code;
        }
    }
}