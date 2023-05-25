using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletWink : BulletBase
    {
        private float Duration_;
        private float Time_;
        
        public BulletWink(GameObject go, CircleArea circle, float duration)
            : base(go, circle)
        {
            Duration_ = duration;
            Time_ = 0;
        }

        public override void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            if (Time_ == 0)
            {
                CheckCollision();
            }

            Time_ += deltaTime;
            if (Time_ >= Duration_)
            {
                Time_ = 0f;
                Dead();
            }
        }
        
        private void CheckCollision()
        {
            var result = PhysicUtils.CheckOverlapEnemy(GetCircle());
            foreach (var enemy in result)
            {
                enemy.OnBulletCollision(this);
            }
        }
    }
}