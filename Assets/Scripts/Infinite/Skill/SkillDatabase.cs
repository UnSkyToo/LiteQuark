using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;

namespace InfiniteGame
{
    public sealed class SkillDatabase : Singleton<SkillDatabase>
    {
        private readonly Dictionary<int, Type> SkillList_ = new Dictionary<int, Type>();

        public SkillDatabase()
        {
            SkillList_.Clear();
            SkillList_.Add(1, typeof(Skill1));
            SkillList_.Add(2, typeof(Skill2));
            SkillList_.Add(3, typeof(Skill3));
            SkillList_.Add(4, typeof(Skill4));
            
            SkillList_.Add(101, typeof(Skill101));
            SkillList_.Add(102, typeof(Skill102));
        }

        public int[] GetRandomList(int count)
        {
            var list = SkillList_.Keys.ToList();
            
            while (list.Count > count)
            {
                list.RemoveAt(MathUtils.RandInt(0, list.Count));
            }

            return list.ToArray();
        }

        public SkillBase GetSkill(int id)
        {
            if (SkillList_.TryGetValue(id, out var skill))
            {
                return Activator.CreateInstance(skill) as SkillBase;
            }

            return null;
        }

        public string GetSkillName(int id)
        {
            return GetSkill(id)?.Name ?? "unknown";
        }
    }
}