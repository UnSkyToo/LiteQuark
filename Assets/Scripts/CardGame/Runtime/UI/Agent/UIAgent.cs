using LiteCard.GamePlay;
using LiteQuark.Runtime;

namespace LiteCard.UI
{
    public sealed class UIAgent : UIBase
    {
        public override string PrefabPath => "CardGame/Prefab/UI/Agent/UIAgent.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;

        private UIItemList<UIPlayerItem, AgentBase> PlayerList_;
        private UIItemList<UIMonsterItem, AgentBase> MonsterList_;

        public UIAgent()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            PlayerList_ = new UIItemList<UIPlayerItem, AgentBase>(
                FindChild("PlayerContent"),
                GameConst.Prefab.PlayerItem);

            MonsterList_ = new UIItemList<UIMonsterItem, AgentBase>(
                FindChild("MonsterContent"),
                GameConst.Prefab.MonsterItem);

            RefreshInfo();
        }

        protected override void OnClose()
        {
            PlayerList_.Dispose();
            MonsterList_.Dispose();
        }

        public void RefreshInfo()
        {
            PlayerList_.RefreshInfo(new AgentBase[] { AgentSystem.Instance.GetPlayer() });
            MonsterList_.RefreshInfo(AgentSystem.Instance.GetMonsterList());
        }
    }
}