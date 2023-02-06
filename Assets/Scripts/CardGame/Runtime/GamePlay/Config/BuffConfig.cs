namespace LiteCard.GamePlay
{
    public sealed class BuffConfig : IJsonMainConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        [EditorDataReadOnly]
        public int ID { get; private set; }
        public string Name { get; private set; }
        public CharacterJob Job { get; private set; }
        public int Number { get; private set; }
        public BuffTriggerType TriggerType { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.BuffTrigger, nameof(TriggerType))]
        public object[] TriggerParams { get; private set; }
        public BuffSustainType SustainType { get; private set; }
        public int MaxLayer { get; private set; }
        [EditorDataPopup(EditorDataPopupType.Match)]
        public int MatchID { get; private set; }
        public ChangeAttrConfig[] BaseAttr { get; private set; }
        public ChangeAttrConfig[] LayerAttr { get; private set; }
        public ModifierSet[] ModifierSets { get; private set; }

        public BuffConfig()
        {
        }

        public int GetMainID()
        {
            return ID;
        }

        public object Clone()
        {
            var result = new BuffConfig
            {
                ID = ID,
                Name = Name,
                Job = Job,
                Number = Number,
                TriggerType = TriggerType,
                TriggerParams = TypeUtils.CloneObjectArray(TriggerParams),
                SustainType = SustainType,
                MaxLayer = MaxLayer,
                MatchID = MatchID,
                BaseAttr = TypeUtils.CloneDataArray(BaseAttr),
                LayerAttr = TypeUtils.CloneDataArray(LayerAttr),
                ModifierSets = TypeUtils.CloneDataArray(ModifierSets)
            };
            return result;
        }
    }
    
    public sealed class BuffSet : IJsonConfig, IJsonMainID
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        [EditorDataPopup(EditorDataPopupType.Buff)]
        public int BuffID { get; private set; }
        public int Layer { get; private set; }

        public BuffSet()
        {
        }

        public int GetMainID()
        {
            return BuffID;
        }

        public object Clone()
        {
            var result = new BuffSet
            {
                BuffID = BuffID,
                Layer = Layer
            };
            return result;
        }
    }
}