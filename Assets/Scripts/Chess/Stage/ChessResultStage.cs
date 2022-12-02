using TMPro;
using UnityEngine.UI;

namespace LiteGamePlay.Chess
{
    public sealed class ChessResultStage : ChessStageBase
    {
        public override string ViewName => "ResultPage";

        public override void OnOpen(params object[] args)
        {
            var win = (ChessKind)args[0];

            GetComponent<TextMeshProUGUI>("LabelResult").text = $"{win} Win!!!";
            GetComponent<Button>("BtnConfirm").onClick.RemoveAllListeners();
            GetComponent<Button>("BtnConfirm").onClick.AddListener(OnBtnConfirmClick);
        }

        public override void OnClose()
        {
            GetComponent<Button>("BtnConfirm").onClick.RemoveAllListeners();
        }

        private void OnBtnConfirmClick()
        {
            ChessStageManager.Instance.ChangeTo<ChessMenuStage>();
        }
    }
}