using LiteCard.GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public sealed class UIBattleMain : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Battle/UIBattleMain.prefab";

        public UIBattleMain()
        {
        }
        
        protected override void OnOpen(params object[] paramList)
        {
            UIUtils.FindComponent<Button>(Go, "BtnNextRound").onClick.AddListener(OnBtnNextRoundClick);
            EventManager.Instance.Register<PlayerEnergyChangeEvent>(OnPlayerEnergyChangeEvent);
            
            RefreshInfo();
        }

        protected override void OnClose()
        {
            UIUtils.FindComponent<Button>(Go, "BtnNextRound").onClick.RemoveListener(OnBtnNextRoundClick);
            EventManager.Instance.UnRegister<PlayerEnergyChangeEvent>(OnPlayerEnergyChangeEvent);
        }

        public void RefreshInfo()
        {
            var player = AgentSystem.Instance.GetPlayer();
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "Energy/LabelValue").text = $"{player.CurEnergy}/{player.MaxEnergy}";
        }
        
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