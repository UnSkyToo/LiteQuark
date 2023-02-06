namespace LiteCard.GamePlay
{
    public sealed class DamageData
    {
        public AgentBase Caster { get; }
        public AgentBase Target { get; }
        public int Value { get; set; }

        public DamageData(AgentBase caster, AgentBase target, int value)
        {
            Caster = caster;
            Target = target;
            Value = value;
        }
    }
}