using System.Linq;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BattleManager : Singleton<BattleManager>
    {
        public bool Pause { get; set; }
        
        private Player Player_;
        private ListEx<Enemy> EnemyList_ = new ListEx<Enemy>();
        private ListEx<BulletBase> BulletList_ = new ListEx<BulletBase>();
        private ListEx<Exp> ExpList_ = new ListEx<Exp>();

        private GameObjectPool EnemyPool_;
        private GameObjectPool ExpPool_;

        private IEnemyEmitter EnemyEmitter_;

        public BattleManager()
        {
            EnemyPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Enemy.prefab");
            ExpPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Exp.prefab");

            EnemyEmitter_ = new NormalEnemyEmitter();

            Pause = false;
        }

        public void Tick(float deltaTime)
        {
            if (Pause)
            {
                return;
            }

            Player_.Tick(deltaTime);
            
            EnemyList_.Foreach((enemy, dt) =>
            {
                enemy.Tick(dt);
            }, deltaTime);

            BulletList_.Foreach((bullet, dt) =>
            {
                bullet.Tick(dt);
            }, deltaTime);
            
            ExpList_.Foreach((exp, dt) =>
            {
                exp.Tick(dt);
            }, deltaTime);

            EnemyEmitter_.Tick(deltaTime);
        }

        public void CreatePlayer()
        {
            var go = LiteRuntime.Get<AssetSystem>().InstantiateSync("Infinite/Prefab/Player.prefab", null);
            Player_ = new Player(go, new CircleArea(0.2f));
            Player_.MoveSpeed = 3;
            Player_.DamageAdd = 0;
            
            Player_.AddSkill(4);
        }

        public Player GetPlayer()
        {
            return Player_;
        }

        public BulletBase[] GetBullets()
        {
            return BulletList_.ToArray();
        }

        public Enemy[] GetEnemies()
        {
            return EnemyList_.ToArray();
        }

        public Exp[] GetExps()
        {
            return ExpList_.ToArray();
        }

        public void CreateEnemy()
        {
            var go = EnemyPool_.Alloc();
            var enemy = new Enemy(go, new CircleArea(0.25f));
            enemy.MoveSpeed = 1.0f;
            enemy.Hp = MathUtils.RandInt(1, 3);
            enemy.IsAlive = true;
            enemy.UpdateHpText();
            enemy.SetPosition(GameUtils.RandomPosition(11));
            EnemyList_.Add(enemy);
        }

        public void RemoveEnemy(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }
            
            EnemyPool_.Recycle(enemy.Go);
            EnemyList_.Remove(enemy);
        }

        public Enemy FindEnemy(int maxRange)
        {
            var minDist = float.MaxValue;
            Enemy target = null;
            
            EnemyList_.Foreach((enemy) =>
            {
                var dist = Vector3.Distance(enemy.GetPosition(), Player_.GetPosition());
                if (dist <= maxRange && dist < minDist)
                {
                    minDist = dist;
                    target = enemy;
                }
            });

            return target;
        }

        public BulletTrack CreateBulletTrack(Vector3 beginPos, Vector3 targetPos, float speed)
        {
            var bullet = BulletFactory.Instance.CreateBulletTrack(beginPos, targetPos, speed);
            BulletList_.Add(bullet);
            return bullet;
        }

        public BulletCurve CreateBulletCurve(CurveData curve, float speed)
        {
            var bullet = BulletFactory.Instance.CreateBulletCurve(curve, speed);
            BulletList_.Add(bullet);
            return bullet;
        }

        public BulletArea CreateBulletArea(int level, float interval)
        {
            var bullet = BulletFactory.Instance.CreateBulletArea(level, interval);
            BulletList_.Add(bullet);
            return bullet;
        }

        public BulletWink CreateBulletWink(Vector3 position, float duration)
        {
            var bullet = BulletFactory.Instance.CreateBulletWink(position, duration);
            BulletList_.Add(bullet);
            return bullet;
        }

        public void RemoveBullet(BulletBase bullet)
        {
            BulletFactory.Instance.RemoveBullet(bullet);
            BulletList_.Remove(bullet);
        }

        public void CreateExp(Vector3 position)
        {
            var go = ExpPool_.Alloc();
            var exp = new Exp(go, new CircleArea(0.1f));
            exp.SetPosition(position);
            exp.Value = 1;
            ExpList_.Add(exp);
        }

        public void RemoveExp(Exp exp)
        {
            if (exp == null)
            {
                return;
            }
            
            ExpPool_.Recycle(exp.Go);
            ExpList_.Remove(exp);
        }
    }
}