using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public class UIFloatText : BaseUI
    {
        private LiteEntity Entity_;
        private ulong ActionID_;

        protected override void OnOpen(params object[] paramList)
        {
            base.OnOpen(paramList);

            Entity_ = paramList[0] as LiteEntity;
            var value = paramList[1] as string;
            FindComponent<TextMeshProUGUI>().text = value;
            FindComponent<TextMeshProUGUI>().color = Color.red;

            var worldPos = Entity_.GetModule<LiteEntityBehaveModule>().GetInternalGo().transform.position + Vector3.up * 2f;
            RT.anchoredPosition = UIUtils.WorldPointToCanvasPoint(worldPos, LiteCameraManager.Instance.MainCamera, RT.parent, LiteRuntime.Get<UISystem>().UICamera);
            
            var act = ActionBuilder.Sequence()
                .RectTransformMove(RT, new Vector2(0f + Random.Range(-10, 10), 100f + Random.Range(-20, 20)), 0.5f, true)
                .TransformFadeOut(RT, 0.5f)
                .Callback(() =>
                {
                    ActionID_ = 0;
                    LiteRuntime.Get<UISystem>().CloseUI(this);
                });
            ActionID_ = LiteRuntime.Action.AddAction(act.Flush());
        }

        protected override void OnClose()
        {
            base.OnClose();

            if (ActionID_ != 0)
            {
                LiteRuntime.Action.StopAction(ActionID_);
                ActionID_ = 0;
            }
        }
    }
}