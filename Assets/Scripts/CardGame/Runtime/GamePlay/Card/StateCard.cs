namespace LiteCard.GamePlay
{
    public sealed class StateCard : CardBase
    {
        public StateCard(CardData data)
            : base(data)
        {
        }

        protected override bool OnCast(Player caster, AgentBase target)
        {
            return false;
        }

        public override CardBase Clone()
        {
            return new StateCard(Data_.Clone());
        }
    }
}