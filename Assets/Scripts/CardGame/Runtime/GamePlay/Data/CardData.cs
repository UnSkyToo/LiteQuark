using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class CardData
    {
        public CardConfig Cfg { get; }
        public CardCastData CastNormal { get; }
        public CardCastData CastUpgrade { get; }
        public int CastCount { get; set; }
        public RecordInt UpgradeLevel { get; set; }
        public RecordInt DecCost { get; set; }
        public RecordList<CardTag> Tags { get; set; }

        public CardData(int id)
        {
            Cfg = LiteRuntime.Get<ConfigSystem>().GetData<CardConfig>(id);
            CastNormal = new CardCastData(Cfg.CastNormal);
            CastUpgrade = new CardCastData(Cfg.CastUpgrade);
            CastCount = 0;
            UpgradeLevel = new RecordInt();
            DecCost = new RecordInt();
            Tags = new RecordList<CardTag>();
            foreach (var tag in Cfg.Tags)
            {
                Tags.Add(RecordScopeType.Battle, tag);
            }
        }

        public void ResetRecord(RecordScopeType scopeType)
        {
            UpgradeLevel.ResetValue(scopeType);
        }

        public CardData Clone()
        {
            var data = new CardData(Cfg.ID);
            data.CastCount = CastCount;
            data.UpgradeLevel = UpgradeLevel.Clone();
            data.Tags = Tags.Clone();
            data.DecCost = DecCost.Clone();
            Tags = Tags.Clone();
            return data;
        }
    }
}