using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class Player : BattleEntity
    {
        public float MoveSpeed { get; set; }
        public  int DamageAdd { get; set; }

        private Dictionary<int, SkillBase> SkillList_;

        public int CurExp { get; private set; }
        public int MaxExp { get; private set; }
        public int Level { get; private set; }

        public Player(GameObject go, CircleArea circle)
            : base(go, circle)
        {
            SkillList_ = new Dictionary<int, SkillBase>();

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
                skill.Value.Tick(deltaTime);
            }
            
            CheckCollision();
            Camera.main.transform.localPosition = new Vector3(GetPosition().x, GetPosition().y, -10);

            if (Input.GetKeyDown(KeyCode.J))
            {
                foreach (var skill in SkillList_)
                {
                    skill.Value.AddLevel();
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

            var skillList = SkillDatabase.Instance.GetRandomList(3);
            LiteRuntime.Get<UISystem>().OpenUI<UIChooseSkill>(skillList);
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

        public void AddSkill(int id)
        {
            if (SkillList_.TryGetValue(id, out var skill))
            {
                skill.AddLevel();
            }
            else
            {
                var newSkill = SkillDatabase.Instance.GetSkill(id);
                SkillList_.Add(id, newSkill);
                newSkill.Attach();
            }
        }
    }
}