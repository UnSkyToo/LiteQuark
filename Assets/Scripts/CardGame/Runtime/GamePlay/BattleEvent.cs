namespace LiteCard.GamePlay
{
    public sealed class CardChangeEvent : EventBase
    {
        public CardChangeEvent()
        {
        }
    }

    public sealed class AgentAttrChangeEvent : EventBase
    {
        public AgentBase Agent { get; }
        public AgentAttrType AttrType { get; }
        public float ChangeValue { get; }

        public AgentAttrChangeEvent(AgentBase agent, AgentAttrType attrType, float changeValue)
        {
            Agent = agent;
            AttrType = attrType;
            ChangeValue = changeValue;
        }
    }

    public sealed class PlayerEnergyChangeEvent : EventBase
    {
        public PlayerEnergyChangeEvent()
        {
        }
    }

    public sealed class BuffLayerChangeEvent : EventBase
    {
        public AgentBase Agent { get; }
        public int BuffID { get; }

        public BuffLayerChangeEvent(AgentBase agent, int buffID)
        {
            Agent = agent;
            BuffID = buffID;
        }
    }
}