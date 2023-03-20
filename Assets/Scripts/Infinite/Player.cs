using UnityEngine;

namespace InfiniteGame
{
    public sealed class Player : MonoBehaviour
    {
        public float AttackInterval;
        public float MoveSpeed;

        private float Interval_;

        private IBulletEmitter Emitter_;
        private Vector3 Position_;

        public int CurExp { get; private set; }
        public int MaxExp { get; private set; }
        public int Level { get; private set; }

        private void Awake()
        {
            AttackInterval = Mathf.Max(AttackInterval, 0.01f);
            Emitter_ = new BulletSerialEmitter(this, 0.083f);

            Interval_ = AttackInterval - 0.2f;
            Position_ = transform.localPosition;

            CurExp = 0;
            MaxExp = 5;
            Level = 1;
        }

        private void FixedUpdate()
        {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            var moveDir = new Vector3(h, v, 0).normalized;
            transform.Translate(moveDir * (MoveSpeed * Time.fixedDeltaTime), Space.Self);
            Position_ = transform.localPosition;
        }

        private void Update()
        {
            Interval_ += Time.deltaTime;
            while (Interval_ >= AttackInterval)
            {
                Interval_ -= AttackInterval;
                Fire();
            }
            
            CheckCollision();
            Camera.main.transform.localPosition = new Vector3(Position_.x, Position_.y, -10);
        }

        public Vector3 GetPosition()
        {
            return Position_;
        }

        private void Fire()
        {
            Emitter_.Fire(BattleManager.Instance.FindEnemy());
        }

        private void AddExp(int val)
        {
            CurExp += val;
            Debug.Log(CurExp);
            
            if (CurExp >= MaxExp)
            {
                Level++;
                DoLevelUp();
            }
        }

        private void DoLevelUp()
        {
            MaxExp += (int)(MaxExp * 1.5f);
            Debug.Log($"Level up, {CurExp}/{MaxExp}");
        }
        
        private void CheckCollision()
        {
            var result = PhysicUtils.CheckOverlapCircle(transform.localPosition, 0.5f, Const.Mask.Collision);
            foreach (var col in result)
            {
                if (col.CompareTag("Exp"))
                {
                    var exp = col.transform.GetComponent<Exp>();
                    AddExp(exp.Value);
                    exp.OnPlayerCollision();
                }
                else if (col.CompareTag("Enemy"))
                {
                    var enemy = col.transform.GetComponent<Enemy>();
                    enemy.OnPlayerCollision(this);
                }
            }
        }
    }
}