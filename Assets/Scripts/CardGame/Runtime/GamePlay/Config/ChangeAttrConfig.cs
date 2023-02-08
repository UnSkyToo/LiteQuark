using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class ChangeAttrConfig : IJsonConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        public AgentAttrType AttrType { get; private set; }
        public int Value { get; private set; }
        public float Percent { get; private set; }

        public ChangeAttrConfig()
        {
        }

        public object Clone()
        {
            var result = new ChangeAttrConfig
            {
                AttrType = AttrType,
                Value = Value,
                Percent = Percent
            };
            return result;
        }
    }
}