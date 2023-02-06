namespace LiteCard.GamePlay
{
    public sealed class ModifierData
    {
        public ModifierConfig Cfg { get; }
        public object[] ParamList { get; }

        public ModifierData(int id, object[] paramList)
        {
            Cfg = ConfigDatabase.Instance.GetData<ModifierConfig>(id);
            ParamList = paramList;
        }
    }
}