namespace LiteCard.GamePlay
{
    public sealed class CardCastData : ExecuteData
    {
        public CardCastConfig Cfg { get; }
        
        public CardCastData(CardCastConfig cfg)
        {
            Cfg = cfg;
            
            InitExecuteData(cfg.ModifierSets);
        }
    }
}