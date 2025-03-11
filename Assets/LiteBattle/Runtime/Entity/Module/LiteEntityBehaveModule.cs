using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityBehaveModule : LiteEntityModuleBase
    {
        private bool IsLoad_;
        private GameObject Go_;
        private Transform TS_;
        private Animator Animator_;
        private int AnimationNameHash_;
        private LiteColliderBinder ColliderBinder_;
        private string PrefabPath_;
        
        public LiteEntityBehaveModule(LiteEntity entity)
            : base(entity)
        {
            IsLoad_ = false;
            AnimationNameHash_ = 0;
        }

        public override void Dispose()
        {
            RecyclePrefab();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsLoad_)
            {
                return;
            }
            UpdateTransform();
            UpdateAnimator();
            // UpdateHUD();
        }

        public GameObject GetInternalGo()
        {
            return Go_;
        }

        private void UpdateTransform()
        {
            TS_.localPosition = Entity.Position;
            TS_.localScale = Entity.Scale;
            TS_.localRotation = Entity.Rotation;
        }

        private void UpdateAnimator()
        {
            if (AnimationNameHash_ == Entity.AnimationNameHash)
            {
                return;
            }

            AnimationNameHash_ = Entity.AnimationNameHash;
            Animator_.CrossFade(AnimationNameHash_, 0, 0);
        }

        private void UpdateHUD()
        {
            var curHp = Entity.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.CurHp);
            var maxHp = Math.Max(1, Entity.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.MaxHp));
            // var hpPercent = Math.Clamp(0d, 1d, curHp / maxHp);
        }
        
        public void LoadPrefab(string prefabPath)
        {
            IsLoad_ = false;
            RecyclePrefab();
            PrefabPath_ = prefabPath;
            
            LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Alloc(null, (go) =>
            {
                Go_ = go;
                TS_ = Go_.transform;
                TS_.localPosition = Vector3.zero;
                TS_.localScale = Vector3.one;
                TS_.localRotation = Quaternion.identity;
                
                Animator_ = Go_.GetComponent<Animator>();

                ColliderBinder_ = Go_.GetOrAddComponent<LiteColliderBinder>();
                if (ColliderBinder_ != null)
                {
                    ColliderBinder_.UniqueID = Entity.UniqueID;
                }

                IsLoad_ = true;
            });
        }

        private void RecyclePrefab()
        {
            if (Go_ != null)
            {
                LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Recycle(Go_);
                Go_ = null;
            }

            PrefabPath_ = string.Empty;
        }
    }
}