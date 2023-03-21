using System.Collections.Generic;

namespace InfiniteGame
{
    public static class PhysicUtils
    {
        public static Enemy CheckOverlapEnemyOne(CircleArea circle)
        {
            foreach (var enemy in BattleManager.Instance.GetEnemies())
            {
                if (enemy.GetCircle().IsOverlap(circle))
                {
                    return enemy;
                }
            }

            return null;
        }

        public static Enemy[] CheckOverlapEnemy(CircleArea circle)
        {
            var result = new List<Enemy>();
            
            foreach (var enemy in BattleManager.Instance.GetEnemies())
            {
                if (enemy.GetCircle().IsOverlap(circle))
                {
                    result.Add(enemy);
                }
            }

            return result.ToArray();
        }

        public static Exp[] CheckOverlapExp(CircleArea circle)
        {
            var result = new List<Exp>();
                        
            foreach (var exp in BattleManager.Instance.GetExps())
            {
                if (exp.GetCircle().IsOverlap(circle))
                {
                    result.Add(exp);
                }
            }

            return result.ToArray();
        }
    }
}