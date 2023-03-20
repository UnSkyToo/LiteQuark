using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletManager : Singleton<BulletManager>
    {
        private GameObjectPool Pool_;
        
        public BulletManager()
        {
            Pool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Bullet.prefab");
        }
        
        public Bullet CreateBullet(Vector3 beginPos)
        {
            var go = Pool_.Alloc();
            go.transform.localPosition = new Vector3(beginPos.x, beginPos.y, 0);
            go.transform.localScale = Vector3.one * 0.5f;
            go.tag = Const.Tag.Bullet;
            
            var ctrl = go.GetOrAddComponent<Bullet>();
            ctrl.IsAlive = true;
            ctrl.LifeTime = 3;

            return ctrl;
        }

        public Bullet CreateBullet(Vector3 beginPos, Vector3 targetPos, float speed)
        {
            var angle = MathUtils.AngleByPoint(beginPos, targetPos);
            var bullet = CreateBullet(beginPos);
            bullet.Angular = angle;
            bullet.Velocity = speed;
            bullet.Damage = 1;
            return bullet;
        }

        public void RemoveBullet(Bullet bullet)
        {
            Pool_.Recycle(bullet.gameObject);
        }
    }
}