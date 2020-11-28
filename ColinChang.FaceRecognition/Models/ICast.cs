namespace ColinChang.FaceRecognition.Models
{
    public interface ICast<out T>
    {
        T Cast();
    }
}