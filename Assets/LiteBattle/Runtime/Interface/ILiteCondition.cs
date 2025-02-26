using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    /// <summary>
    /// <para>TransferEvent condition interface</para>
    /// <para>Must be in the same assembly as the interface</para>
    /// </summary>
    public interface ILiteCondition : IHasData, IClone<ILiteCondition>
    {
        bool Check(LiteState state);
    }
}