using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class ModifierData
    {
        public ModifierConfig Cfg { get; }
        public object[] ParamList { get; }

        public ModifierData(int id, object[] paramList)
        {
            Cfg = LiteRuntime.Get<ConfigSystem>().GetData<ModifierConfig>(id);
            ParamList = paramList;
        }
    }
}