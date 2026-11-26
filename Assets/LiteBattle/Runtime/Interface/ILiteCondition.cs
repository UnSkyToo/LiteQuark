using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    /// <summary>Condition evaluated by a state transition event.</summary>
    public interface ILiteCondition : IHasData, IClone<ILiteCondition>
    {
        bool Check(LiteState state);
    }
}
