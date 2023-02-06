namespace LiteCard.GamePlay
{
    public sealed class ArmourData
    {
        public AgentBase Caster { get; }
        public AgentBase Target { get; }
        public int Value { get; set; }

        public ArmourData(AgentBase caster, AgentBase target, int value)
        {
            Caster = caster;
            Target = target;
            Value = value;
        }
    }
}