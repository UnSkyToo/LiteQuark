using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletFactory : Singleton<BulletFactory>
    {
        private GameObjectPool Bullet1Pool_;
        private GameObjectPool Bullet3Pool_;
        
        public BulletFactory()
        {
        }

        public void Initialize()
        {
            Bullet1Pool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Bullet.prefab");
            Bullet3Pool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Bullet3.prefab");
        }
        
        public BulletTrack CreateBulletTrack(Vector3 beginPos, Vector3 targetPos, float speed)
        {
            var go = Bullet1Pool_.Alloc();
            go.tag = Const.Tag.Bullet;

            var bullet = new BulletTrack(go, new CircleArea(0.08f));
            bullet.Pool = Bullet1Pool_;
            bullet.SetPosition(beginPos);
            bullet.LifeTime = 3;
            bullet.Angular = MathUtils.AngleByPoint(beginPos, targetPos);
            bullet.AngularAcceleration = 0;
            bullet.AngularAccelerationDelay = 0;
            bullet.Velocity = speed;
            bullet.Acceleration = 0;
            bullet.AccelerationDelay = 0;
            bullet.Damage = 1;
            
            return bullet;
        }

        public BulletCurve CreateBulletCurve(CurveData curve, float speed)
        {
            var go = Bullet1Pool_.Alloc();
            go.tag = Const.Tag.Bullet;

            var bullet = new BulletCurve(go, new CircleArea(0.08f), curve, speed);
            bullet.Pool = Bullet1Pool_;
            bullet.Damage = 1;
            
            return bullet;
        }

        public BulletArea CreateBulletArea(int level, float interval)
        {
            var scale = 1f + (level - 1) * 0.3f;
            
            var go = LiteRuntime.Get<AssetSystem>().InstantiateSync("Infinite/Prefab/Bullet2.prefab", null);
            go.tag = Const.Tag.Bullet;
            go.transform.localScale = Vector3.one * scale;

            var bullet = new BulletArea(go, new CircleArea(0.64f * scale), interval);
            bullet.Damage = 1;
            
            return bullet;
        }

        public BulletWink CreateBulletWink(Vector3 position, float duration)
        {
            var go = Bullet3Pool_.Alloc();
            go.tag = Const.Tag.Bullet;

            var bullet = new BulletWink(go, new CircleArea(0.08f), duration);
            bullet.Pool = Bullet3Pool_;
            bullet.SetPosition(position);
            bullet.Damage = 1;

            return bullet;
        }

        public void RemoveBullet(BulletBase bullet)
        {
            if (bullet == null)
            {
                return;
            }

            if (bullet.Pool != null)
            {
                bullet.Pool.Recycle(bullet.Go);
            }
            else
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(bullet.Go);
            }
        }
    }
}