using UnityEngine.UI;

namespace LiteGamePlay.Chess
{
    public sealed class ChessMenuStage : ChessStageBase
    {
        public override string ViewName => "MenuPage";

        public override void OnOpen(params object[] args)
        {
            GetComponent<Button>("BtnStart").onClick.RemoveAllListeners();
            GetComponent<Button>("BtnStart").onClick.AddListener(OnBtnStartClick);
        }

        public override void OnClose()
        {
            GetComponent<Button>("BtnStart").onClick.RemoveAllListeners();
        }

        private void OnBtnStartClick()
        {
            ChessStageManager.Instance.ChangeTo<ChessGameStage>();
        }
    }
}