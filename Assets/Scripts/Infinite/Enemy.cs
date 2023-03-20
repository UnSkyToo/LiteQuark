using System.Collections;
using TMPro;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class Enemy : MonoBehaviour
    {
        public bool IsAlive { get; set; }
        
        public float MoveSpeed;

        public int Hp;
        
        private TextMeshPro HpText_;
        
        private void Awake()
        {
            HpText_ = GetComponentInChildren<TextMeshPro>();
            UpdateHpText();
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            var player = BattleManager.Instance.GetPlayer();
            if (Vector3.Distance(player.GetPosition(), transform.localPosition) < 0.1f)
            {
                return;
            }

            var moveDir = (player.GetPosition() - transform.localPosition).normalized;
            transform.Translate(moveDir * (MoveSpeed * Time.fixedDeltaTime), Space.Self);
        }

        public void UpdateHpText()
        {
            HpText_.text = $"{Hp}";
        }

        public void OnBulletCollision(Bullet bullet)
        {
            Hp--;
            UpdateHpText();

            if (Hp <= 0)
            {
                Dead();
            }
            else
            {
                StartCoroutine(Twinkle());
            }
        }

        public void OnPlayerCollision(Player player)
        {
            Dead();
        }

        private IEnumerator Twinkle()
        {
            GetComponent<SpriteRenderer>().color = Color.red;
            yield return new WaitForSeconds(0.05f);
            GetComponent<SpriteRenderer>().color = Color.white;
        }

        private void Dead()
        {
            IsAlive = false;
            BattleManager.Instance.CreateExp(transform.localPosition);
            BattleManager.Instance.RemoveEnemy(this);
        }
    }
}