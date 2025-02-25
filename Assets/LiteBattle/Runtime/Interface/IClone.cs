namespace LiteBattle.Runtime
{
    public interface IClone<out T>
    {
        T Clone();
    }
}