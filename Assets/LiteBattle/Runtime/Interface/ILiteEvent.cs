namespace LiteBattle.Runtime
{
    public enum LiteEventSignal : byte
    {
        Continue,
        Break,
    }
    
    /// <summary>
    /// <para>Clip event interface</para>
    /// <para>Must be in the same assembly as the interface</para>
    /// </summary>
    public interface ILiteEvent : IHasData, IClone<ILiteEvent>
    {
        void Enter(LiteState state);
        LiteEventSignal Tick(LiteState state, float deltaTime);
        void Leave(LiteState state);
    }
}