using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class CardCastConfig : IJsonConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        public string Desc { get; private set; }
        public int Cost { get; private set; }
        [EditorDataPopup(EditorDataPopupType.Match)]
        public int MatchID { get; private set; }
        public ModifierSet[] ModifierSets { get; private set; }
        public CardCastTargetType TargetType { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.CardCastTarget, nameof(TargetType))]
        public object[] TargetParams { get; private set; }
        public CardCastCheckType CheckType { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.CardCastCheck, nameof(CheckType))]
        public object[] CheckParams { get; private set; }
        
        public CardCastConfig()
        {
        }

        public object Clone()
        {
            var result = new CardCastConfig
            {
                Desc = Desc,
                Cost = Cost,
                MatchID = MatchID,
                ModifierSets = TypeUtils.CloneDataArray(ModifierSets),
                TargetType = TargetType,
                TargetParams = TypeUtils.CloneObjectArray(TargetParams),
                CheckType = CheckType,
                CheckParams = TypeUtils.CloneObjectArray(CheckParams),
            };
            return result;
        }
    }
}