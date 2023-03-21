using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletTrack : BulletBase
    {
        public float Angular;
        public float AngularAcceleration;
        public float AngularAccelerationDelay;
        public float Velocity;
        public float Acceleration;
        public float AccelerationDelay;

        public float LifeTime;

        public BulletTrack(GameObject go, CircleArea circle)
            : base(go, circle)
        {
            LifeTime = 3;
        }

        public override void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            LifeTime -= deltaTime;
            if (LifeTime <= 0)
            {
                Dead();
                return;
            }

            var direction = (Quaternion.AngleAxis(Angular, Vector3.back) * Vector3.up).normalized;
            var position = GetPosition() + direction * (Velocity * deltaTime);
            SetPosition(position);

            AngularAccelerationDelay -= deltaTime;
            if (AngularAccelerationDelay <= 0)
            {
                Angular += AngularAcceleration * deltaTime;
            }

            AccelerationDelay -= deltaTime;
            if (AccelerationDelay <= 0)
            {
                Velocity += Acceleration * deltaTime;
            }

            CheckCollision();
        }

        private void CheckCollision()
        {
            var enemy = PhysicUtils.CheckOverlapEnemyOne(GetCircle());
            if (enemy != null)
            {
                enemy.OnBulletCollision(this);
                Dead();
            }
        }
    }
}