using LiteCard.GamePlay;
using LiteQuark.Runtime;
using TMPro;

namespace LiteCard.UI
{
    public sealed class UIBattleMain : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Battle/UIBattleMain.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;

        public UIBattleMain()
        {
        }
        
        protected override void OnOpen(params object[] paramList)
        {
            LiteRuntime.Get<EventSystem>().Register<PlayerEnergyChangeEvent>(OnPlayerEnergyChangeEvent);
            
            RefreshInfo();
        }

        protected override void OnClose()
        {
            LiteRuntime.Get<EventSystem>().UnRegister<PlayerEnergyChangeEvent>(OnPlayerEnergyChangeEvent);
        }

        public void RefreshInfo()
        {
            var player = AgentSystem.Instance.GetPlayer();
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "Energy/LabelValue").text = $"{player.CurEnergy}/{player.MaxEnergy}";
        }
        
        [UIClickEvent("BtnNextRound")]
        private void OnBtnNextRoundClick()
        {
            GameLogic.Instance.GetBattleLogic().RoundEnd();
            GameLogic.Instance.GetBattleLogic().RoundBegin();
        }

        private void OnPlayerEnergyChangeEvent(PlayerEnergyChangeEvent evt)
        {
            RefreshInfo();
        }
    }
}