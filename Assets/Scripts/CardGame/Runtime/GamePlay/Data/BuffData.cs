using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class BuffData : ExecuteData
    {
        public BuffConfig Cfg { get; }
        public AgentBase Caster { get; }
        public int Layer { get; set; }

        public BuffData(int id, AgentBase caster)
        {
            Cfg = LiteRuntime.Get<ConfigSystem>().GetData<BuffConfig>(id);
            Caster = caster;
            Layer = 0;
            
            InitExecuteData(Cfg.ModifierSets);
        }
    }
}