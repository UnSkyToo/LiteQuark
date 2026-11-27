using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityBehaveModule : LiteEntityModuleBase
    {
        private bool IsLoad_;
        private Transform TS_;
        private Animator Animator_;
        private int AnimationNameHash_;
        private LiteColliderBinder ColliderBinder_;
        private GameObjectLease PrefabLease_;
        private bool IsDisposed_;
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
            IsDisposed_ = true;

            if (HUD_ != null)
            {
                LiteRuntime.Get<UISystem>().CloseUI(HUD_);
                HUD_ = null;
            }

            foreach (var effectID in EffectList_)
            {
                LiteRuntime.Get<EffectSystem>().StopEffect(effectID);
            }
            EffectList_.Clear();

            ReleasePrefab();
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
            return PrefabLease_?.Value;
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
                Debug.LogError($"Attachment point '{hangPoint}' not found.");
                return;
            }

            createInfo.SetParent(parent);
            var effectID = LiteRuntime.Get<EffectSystem>().PlayEffect(createInfo);
            if (createInfo.IsLoop)
            {
                EffectList_.Add(effectID);
            }
        }

        public void LoadPrefab(string prefabPath)
        {
            IsLoad_ = false;
            ReleasePrefab();
            var pool = LiteRuntime.ObjectPool.GetActiveGameObjectPool(prefabPath);
            pool.Alloc(null, lease =>
            {
                if (lease == null)
                {
                    return;
                }
                if (IsDisposed_)
                {
                    lease.Dispose();
                    return;
                }
                PrefabLease_ = lease;
                var go = lease.Value;
                TS_ = go.transform;
                TS_.localPosition = Vector3.zero;
                TS_.localScale = Vector3.one;
                TS_.localRotation = Quaternion.identity;

                Animator_ = go.GetComponent<Animator>();

                ColliderBinder_ = go.GetOrAddComponent<LiteColliderBinder>();
                if (ColliderBinder_ != null)
                {
                    ColliderBinder_.UniqueID = Entity.UniqueID;
                }

                LiteRuntime.Get<UISystem>().OpenUI<UINameplateHUD>(UIConfigs.UINameplateHUD, Entity.LifetimeToken, Entity)
                    .ContinueWith(ui => HUD_ = ui).Forget();

                IsLoad_ = true;
                PlayCacheEffect();
            });
        }

        private void ReleasePrefab()
        {
            IsLoad_ = false;
            var lease = PrefabLease_;
            PrefabLease_ = null;
            lease?.Dispose();
        }
    }
}
