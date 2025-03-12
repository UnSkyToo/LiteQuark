﻿using System;
using LiteQuark.Runtime;
using LiteQuark.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LiteBattle.Runtime
{
    public class UINameplateHUD : BaseUI
    {
        public override string PrefabPath => "demo/Prefabs/Nameplate.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Scene;
        public override bool IsMutex => false;
        public override bool AutoAdapt => false;

        private LiteEntity Entity_;

        protected override void OnOpen(params object[] paramList)
        {
            base.OnOpen(paramList);

            Entity_ = paramList[0] as LiteEntity;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            var worldPos = Entity_.GetModule<LiteEntityBehaveModule>().GetInternalGo().transform.position + Vector3.up * 2f;
            RT.anchoredPosition = UIUtils.WorldPointToCanvasPoint(worldPos, LiteCameraManager.Instance.MainCamera, RT.parent, LiteRuntime.Get<UISystem>().UICamera);
            
            var curHp = Entity_.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.CurHp);
            var maxHp = Math.Max(1, Entity_.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.MaxHp));
            var curHpPercent = curHp / maxHp;
            FindComponent<Slider>("HpBar").value = Mathf.Clamp01((float)curHpPercent);
        }
    }
}