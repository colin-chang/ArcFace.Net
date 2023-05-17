namespace ColinChang.ArcFace.Abstraction.Models
{
    public interface ICast<out T>
    {
        T Cast();
    }
}