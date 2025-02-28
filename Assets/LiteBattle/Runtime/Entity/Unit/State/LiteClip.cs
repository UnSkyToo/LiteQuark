namespace LiteBattle.Runtime
{
    public enum LiteClipKind
    {
        Signal,
        Duration,
    }

    public sealed class LiteClip
    {
        public LiteClipKind Kind { get; }
        public float Start { get; }
        public float Duration { get; }

        public ILiteEvent Event { get; }

        public LiteClip(LiteClipKind kind, float start, float duration, ILiteEvent evt)
        {
            Kind = kind;
            Start = start;
            Duration = duration;
#if UNITY_EDITOR
            if (LiteNexusConfig.Instance.QuickReload)
            {
                Event = LiteNexusConfig.Instance.QuickReload ? evt : evt.Clone();
            }
            else
            {
                Event = evt.Clone();
            }
#else
            Event = evt.Clone();
#endif
        }

        public bool InRange(float time)
        {
            return time >= Start && time <= Start + Duration;
        }

        public void Enter(LiteState state)
        {
            // LiteLog.Info($"enter event : {Event.GetType()}");
            Event?.Enter(state);
        }

        public void Leave(LiteState state)
        {
            // LiteLog.Info($"leave event : {Event.GetType()}");
            Event?.Leave(state);
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            return Event?.Tick(state, deltaTime) ?? LiteEventSignal.Continue;
        }
    }
}