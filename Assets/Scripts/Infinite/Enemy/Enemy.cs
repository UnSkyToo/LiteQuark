using System.Collections;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class Enemy : BattleEntity
    {
        public bool IsAlive { get; set; }
        public float MoveSpeed { get; set; }
        public int Hp { get; set; }
        
        private TextMeshPro HpText_;

        public Enemy(GameObject go, CircleArea circle)
            : base(go, circle)
        {
            HpText_ = Go.transform.Find("LabelHp").GetComponent<TextMeshPro>();
            UpdateHpText();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            var player = BattleManager.Instance.GetPlayer();
            if (Vector3.Distance(player.GetPosition(), GetPosition()) < 0.1f)
            {
                return;
            }

            var moveDir = (player.GetPosition() - GetPosition()).normalized;
            var position = GetPosition() + moveDir * (MoveSpeed * deltaTime);
            SetPosition(position);
        }

        public void UpdateHpText()
        {
            HpText_.text = $"{Hp}";
        }

        public void OnBulletCollision(BulletBase bullet)
        {
            Hp = Mathf.Max(0, Hp - bullet.Damage);
            UpdateHpText();

            if (Hp <= 0)
            {
                Dead();
            }
            else
            {
                LiteRuntime.Get<TaskSystem>().AddTask(Twinkle());
            }
        }

        public void OnPlayerCollision(Player player)
        {
            Dead();
        }

        private IEnumerator Twinkle()
        {
            Go.GetComponent<SpriteRenderer>().color = Color.red;
            yield return new WaitForSeconds(0.05f);
            Go.GetComponent<SpriteRenderer>().color = Color.white;
        }

        private void Dead()
        {
            IsAlive = false;
            BattleManager.Instance.CreateExp(GetPosition());
            BattleManager.Instance.RemoveEnemy(this);
        }
    }
}