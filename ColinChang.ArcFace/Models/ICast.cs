namespace ColinChang.ArcFace.Models
{
    public interface ICast<out T>
    {
        T Cast();
    }
}