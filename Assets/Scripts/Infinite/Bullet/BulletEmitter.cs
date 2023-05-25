using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public interface IBulletEmitter
    {
        void Move(Vector3 position);
        void Fire(object data);
    }

    public sealed class BulletCircleEmitter : IBulletEmitter
    {
        private readonly List<BulletCurve> Bullets_;
        private readonly CurveData Curve_;
        
        public BulletCircleEmitter()
        {
            Bullets_ = new List<BulletCurve>();
            Curve_ = LiteRuntime.Get<AssetSystem>().LoadAssetSync<CurveData>("Infinite/Curve/curve_circle.asset");
        }

        public void Fire(object data)
        {
            if (data is not int count)
            {
                return;
            }

            Clean();
            for (var index = 0; index < count; ++index)
            {
                CreateBulletCurve((float)index / (float)count);
            }
        }

        public void Move(Vector3 position)
        {
            foreach (var bullet in Bullets_)
            {
                bullet.SetOffset(position);
            }
        }

        private void Clean()
        {
            foreach (var bullet in Bullets_)
            {
                BattleManager.Instance.RemoveBullet(bullet);
            }
            Bullets_.Clear();
        }

        private void CreateBulletCurve(float percent)
        {
            var bullet = BattleManager.Instance.CreateBulletCurve(Curve_, 10);
            var lerpTime = bullet.GetMaxLerpTime() * percent;
            bullet.SetLerpTime(lerpTime);
            Bullets_.Add(bullet);
        }
    }
    
    public sealed class BulletAreaEmitter : IBulletEmitter
    {
        private BulletArea Bullet_;
        
        public BulletAreaEmitter()
        {
        }

        public void Fire(object data)
        {
            if (data is not int level)
            {
                return;
            }

            Clean();
            Bullet_ = BattleManager.Instance.CreateBulletArea(level, 0.25f);
        }

        public void Move(Vector3 position)
        {
            Bullet_?.SetPosition(position);
        }

        private void Clean()
        {
            BattleManager.Instance.RemoveBullet(Bullet_);
        }
    }
}