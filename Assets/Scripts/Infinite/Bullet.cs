using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public class Bullet : MonoBehaviour
    {
        public bool IsAlive { get; set; }

        public float Angular;
        public float AngularAcceleration;
        public float AngularAccelerationDelay;
        public float Velocity;
        public float Acceleration;
        public float AccelerationDelay;

        public int Damage;
        
        public float LifeTime;

        private void Start()
        {
            IsAlive = true;
            LifeTime = 3;
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            LifeTime -= Time.fixedDeltaTime;
            if (LifeTime <= 0)
            {
                Dead();
                return;
            }

            var direction = (Quaternion.AngleAxis(Angular, Vector3.back) * Vector3.up).normalized;
            transform.Translate(direction * (Velocity * Time.fixedDeltaTime), Space.Self);

            AngularAccelerationDelay -= Time.fixedDeltaTime;
            if (AngularAccelerationDelay <= 0)
            {
                Angular += AngularAcceleration * Time.fixedDeltaTime;
            }

            AccelerationDelay -= Time.fixedDeltaTime;
            if (AccelerationDelay <= 0)
            {
                Velocity += Acceleration * Time.fixedDeltaTime;
            }

            CheckCollision();
        }

        private void Dead()
        {
            IsAlive = false;
            BulletManager.Instance.RemoveBullet(this);
        }

        private void CheckCollision()
        {
            var result = PhysicUtils.CheckOverlapCircleOne(transform.localPosition, 0.16f, Const.Mask.Collision, "Enemy");
            if (result != null)
            {
                var enemy = result.transform.GetComponent<Enemy>();
                enemy.OnBulletCollision(this);
                Dead();
            }
        }
    }
}