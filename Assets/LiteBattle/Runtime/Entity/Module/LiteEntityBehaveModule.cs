using System.Collections.Generic;
using LiteQuark.Runtime;
using LiteQuark.Runtime.UI;
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
        private UINameplateHUD HUD_;
        private Queue<(string, EffectCreateInfo)> CacheEffect_ = new();
        private List<ulong> EffectList_ = new();
        
        public LiteEntityBehaveModule(LiteEntity entity)
            : base(entity)
        {
            IsLoad_ = false;
            AnimationNameHash_ = 0;
        }

        public override void Dispose()
        {
            if (HUD_ != null)
            {
                LiteRuntime.Get<UISystem>().CloseUI(HUD_);
                HUD_ = null;
            }

            foreach (var effectID in EffectList_)
            {
                LiteRuntime.Effect.StopEffect(effectID);
            }
            EffectList_.Clear();

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
            Animator_.CrossFade(AnimationNameHash_, 0.1f, 0);
        }

        private void PlayCacheEffect()
        {
            while (CacheEffect_.Count > 0)
            {
                var (hangPoint, createInfo) = CacheEffect_.Dequeue();
                InternalPlayEffect(hangPoint, createInfo);
            }
        }

        public void PlayEffect(string hangPoint, EffectCreateInfo createInfo)
        {
            if (!IsLoad_)
            {
                CacheEffect_.Enqueue((hangPoint, createInfo));
            }
            else
            {
                InternalPlayEffect(hangPoint, createInfo);
            }
        }

        private void InternalPlayEffect(string hangPoint, EffectCreateInfo createInfo)
        {
            var parent = string.IsNullOrWhiteSpace(hangPoint) ? TS_ : TS_.Find(hangPoint);
            if (parent == null)
            {
                Debug.LogError($"{hangPoint} is not exist!");
                return;
            }

            createInfo.SetParent(parent);
            var effectID = LiteRuntime.Effect.PlayEffect(createInfo);
            if (createInfo.IsLoop)
            {
                EffectList_.Add(effectID);
            }
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

                HUD_ = LiteRuntime.Get<UISystem>().OpenUI<UINameplateHUD>(Entity);

                IsLoad_ = true;
                PlayCacheEffect();
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