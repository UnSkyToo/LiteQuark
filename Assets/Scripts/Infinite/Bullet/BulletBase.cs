using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public abstract class BulletBase : BattleEntity
    {
        public GameObjectPool Pool;
        
        public bool IsAlive { get; protected set; }
        public int Damage { get; set; }

        protected BulletBase(GameObject go, CircleArea circle)
            : base(go, circle)
        {
            IsAlive = true;
        }

        public void Dead()
        {
            IsAlive = false;
            BattleManager.Instance.RemoveBullet(this);
        }
    }
}