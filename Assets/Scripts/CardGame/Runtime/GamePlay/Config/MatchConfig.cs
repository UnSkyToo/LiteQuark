using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class MatchConfig : IJsonMainConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        public int ID { get; private set; }
        public string Name { get; private set; }
        public MatchTargetType TargetType { get; private set; }
        public MatchFilterType FilterType { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.MatchFilter, nameof(FilterType))]
        public object[] FilterParams { get; private set; }
        public int TargetNum { get; private set; }
        
        public MatchConfig()
        {
        }

        public int GetMainID()
        {
            return ID;
        }

        public object Clone()
        {
            var result = new MatchConfig
            {
                ID = ID,
                Name = Name,
                TargetType = TargetType,
                FilterType = FilterType,
                FilterParams = ArrayUtils.CloneObjectArray(FilterParams),
                TargetNum = TargetNum
            };
            return result;
        }
    }
}