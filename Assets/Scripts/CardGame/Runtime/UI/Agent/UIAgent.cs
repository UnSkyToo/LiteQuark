using LiteCard.GamePlay;

namespace LiteCard.UI
{
    public sealed class UIAgent : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Agent/UIAgent.prefab";

        private UIItemList<UIPlayerItem, AgentBase> PlayerList_;
        private UIItemList<UIMonsterItem, AgentBase> MonsterList_;

        public UIAgent()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            PlayerList_ = new UIItemList<UIPlayerItem, AgentBase>(
                UIUtils.FindChild(Go, "PlayerContent"),
                GameConst.Prefab.PlayerItem);

            MonsterList_ = new UIItemList<UIMonsterItem, AgentBase>(
                UIUtils.FindChild(Go, "MonsterContent"),
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