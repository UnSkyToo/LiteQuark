using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletArea : BulletBase
    {
        private float Interval_;
        private float Time_;
        
        public BulletArea(GameObject go, CircleArea circle, float interval)
            : base(go, circle)
        {
            Interval_ = interval;
            Time_ = interval;
        }

        public override void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            Time_ += deltaTime;
            if (Time_ >= Interval_)
            {
                Time_ = 0f;
                CheckCollision();
            }
        }
        
        private void CheckCollision()
        {
            var enemy = PhysicUtils.CheckOverlapEnemyOne(GetCircle());
            if (enemy != null)
            {
                enemy.OnBulletCollision(this);
            }
        }
    }
}