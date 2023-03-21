using System.Collections.Generic;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class Player : BattleEntity
    {
        public float MoveSpeed { get; set; }

        private List<SkillBase> SkillList_;

        public int CurExp { get; private set; }
        public int MaxExp { get; private set; }
        public int Level { get; private set; }

        public Player(GameObject go, CircleArea circle)
            : base(go, circle)
        {
            SkillList_ = new List<SkillBase>();

            CurExp = 0;
            MaxExp = 5;
            Level = 1;
        }

        public override void Tick(float deltaTime)
        {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            var moveDir = new Vector3(h, v, 0).normalized;
            var position = GetPosition() + moveDir * (MoveSpeed * deltaTime);
            SetPosition(position);

            foreach (var skill in SkillList_)
            {
                skill.Tick(deltaTime);
            }
            
            CheckCollision();
            Camera.main.transform.localPosition = new Vector3(GetPosition().x, GetPosition().y, -10);

            if (Input.GetKeyDown(KeyCode.J))
            {
                foreach (var skill in SkillList_)
                {
                    skill.AddLevel();
                }
            }
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
            var enemy = PhysicUtils.CheckOverlapEnemyOne(GetCircle());
            if (enemy != null)
            {
                enemy.OnPlayerCollision(this);
            }

            var exps = PhysicUtils.CheckOverlapExp(GetCircle());
            foreach (var exp in exps)
            {
                AddExp(exp.Value);
                exp.OnPlayerCollision();
            }
        }

        public void AddSkill(SkillBase skill)
        {
            SkillList_.Add(skill);
            skill.Attach();
        }
    }
}