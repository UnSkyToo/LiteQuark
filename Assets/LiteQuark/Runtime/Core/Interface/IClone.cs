namespace LiteQuark.Runtime
{
    public interface IClone<out T>
    {
        T Clone();
    }
}