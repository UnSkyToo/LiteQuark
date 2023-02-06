namespace LiteCard.GamePlay
{
    public sealed class ModifierConfig : IJsonMainConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        public int ID { get; private set; }
        public string Name { get; private set; }
        public ModifierType Type { get; private set; }
        [EditorDataPopup(EditorDataPopupType.Match)]
        public int MatchID { get; private set; }
        public string RepeatCount { get; private set; }

        public ModifierConfig()
        {
        }

        public int GetMainID()
        {
            return ID;
        }

        public object Clone()
        {
            var result = new ModifierConfig
            {
                ID = ID,
                Name = Name,
                Type = Type,
                MatchID = MatchID,
                RepeatCount = RepeatCount
            };
            return result;
        }
    }

    public sealed class ModifierSet : IJsonConfig, IJsonMainID
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        [EditorDataPopup(EditorDataPopupType.Modifier)]
        public int ModifierID { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.Modifier, nameof(ModifierID))]
        public object[] ParamList { get; private set; }

        public ModifierSet()
        {
        }

        public int GetMainID()
        {
            return ModifierID;
        }

        public object Clone()
        {
            var result = new ModifierSet
            {
                ModifierID = ModifierID,
                ParamList = TypeUtils.CloneObjectArray(ParamList)
            };
            return result;
        }
    }
}