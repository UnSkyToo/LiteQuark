using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BattleManager : Singleton<BattleManager>
    {
        private float EnemySpawnerTime_;
        
        private Player Player_;
        private List<Enemy> EnemyList_ = new List<Enemy>();
        private List<Exp> ExpList_ = new List<Exp>();

        private GameObjectPool EnemyPool_;
        private GameObjectPool ExpPool_;

        public BattleManager()
        {
            EnemySpawnerTime_ = 3;
            EnemyPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Enemy.prefab");
            ExpPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Exp.prefab");
        }

        public void Tick(float deltaTime)
        {
            EnemySpawnerTime_ += deltaTime;
            if (EnemySpawnerTime_ > 3f)
            {
                EnemySpawnerTime_ -= 3f;

                for (var i = 0; i < 10; ++i)
                {
                    CreateEnemy();
                }
            }
        }

        public void CreatePlayer()
        {
            var go = LiteRuntime.Get<AssetSystem>().InstantiateSync("Infinite/Prefab/Player.prefab");
            Player_ = go.GetComponent<Player>();
            Player_.AttackInterval = 1;
            Player_.MoveSpeed = 4;
        }

        public Player GetPlayer()
        {
            return Player_;
        }

        private void CreateEnemy()
        {
            var go = EnemyPool_.Alloc();
            go.transform.localPosition = GameUtils.RandomPosition(11);
            var enemy = go.GetComponent<Enemy>();
            enemy.MoveSpeed = 1.0f;
            enemy.Hp = 1;
            enemy.IsAlive = true;
            enemy.UpdateHpText();
            EnemyList_.Add(enemy);
        }

        public void RemoveEnemy(Enemy enemy)
        {
            EnemyPool_.Recycle(enemy.gameObject);
            EnemyList_.Remove(enemy);
        }

        public Enemy FindEnemy()
        {
            var minDist = float.MaxValue;
            Enemy target = null;
            
            foreach (var enemy in EnemyList_)
            {
                var dist = Vector3.Distance(enemy.transform.localPosition, Player_.transform.localPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = enemy;
                }
            }

            return target;
        }

        public void CreateExp(Vector3 position)
        {
            var go = ExpPool_.Alloc();
            go.transform.localPosition = position;
            var exp = go.GetComponent<Exp>();
            exp.Value = 1;
            ExpList_.Add(exp);
        }

        public void RemoveExp(Exp exp)
        {
            ExpPool_.Recycle(exp.gameObject);
            ExpList_.Remove(exp);
        }
    }
}