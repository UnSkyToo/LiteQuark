using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public enum LiteEventSignal : byte
    {
        Continue,
        Break,
    }

    /// <summary>Event data for a Timeline clip or marker.</summary>
    public interface ILiteEvent : IHasData, IClone<ILiteEvent>
    {
        void Enter(LiteState state);
        LiteEventSignal Tick(LiteState state, float deltaTime);
        void Leave(LiteState state);
    }
}
