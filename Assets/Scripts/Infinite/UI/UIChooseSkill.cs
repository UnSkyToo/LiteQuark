using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class UIChooseSkill : BaseUI
    {
        public override string PrefabPath => "Infinite/Prefab/UIChooseSkill.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;
        public override bool IsMutex => true;

        [UIComponent("Content/BtnChoose1/LabelText")]
        private TextMeshProUGUI LabelChoose1_;
        [UIComponent("Content/BtnChoose2/LabelText")]
        private TextMeshProUGUI LabelChoose2_;
        [UIComponent("Content/BtnChoose3/LabelText")]
        private TextMeshProUGUI LabelChoose3_;

        private int[] SkillList_;

        public UIChooseSkill()
            : base()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            var count = paramList.Length;
            SkillList_ = new int[count];

            for (var index = 0; index < 3; ++index)
            {
                if (index < count)
                {
                    SetActive($"Content/BtnChoose{index + 1}", true);
                    SkillList_[index] = (int)paramList[index];
                    FindComponent<TextMeshProUGUI>($"Content/BtnChoose{index + 1}/LabelText").text = $"{SkillDatabase.Instance.GetSkillName(SkillList_[index])}";
                }
                else
                {
                    SetActive($"Content/BtnChoose{index + 1}", false);
                }
            }

            BattleManager.Instance.Pause = true;
        }

        [UIClickEvent("Content/BtnChoose1")]
        private void OnChoose1()
        {
            Choose(0);
        }

        [UIClickEvent("Content/BtnChoose2")]
        private void OnChoose2()
        {
            Choose(1);
        }

        [UIClickEvent("Content/BtnChoose3")]
        private void OnChoose3()
        {
            Choose(2);
        }

        private void Choose(int index)
        {
            LiteRuntime.Get<UISystem>().CloseUI(this);
            Debug.LogWarning($"choose {index + 1}");

            var skillID = SkillList_[index];
            
            BattleManager.Instance.GetPlayer().AddSkill(skillID);
            BattleManager.Instance.Pause = false;
        }
    }
}