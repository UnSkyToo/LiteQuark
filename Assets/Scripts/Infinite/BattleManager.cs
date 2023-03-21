using System.Linq;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BattleManager : Singleton<BattleManager>
    {
        private Player Player_;
        private ListEx<Enemy> EnemyList_ = new ListEx<Enemy>();
        private ListEx<BulletBase> BulletList_ = new ListEx<BulletBase>();
        private ListEx<Exp> ExpList_ = new ListEx<Exp>();

        private GameObjectPool EnemyPool_;
        private GameObjectPool BulletPool_;
        private GameObjectPool ExpPool_;

        private IEnemyEmitter EnemyEmitter_;

        public BattleManager()
        {
            EnemyPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Enemy.prefab");
            BulletPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Bullet.prefab");
            ExpPool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/Exp.prefab");

            EnemyEmitter_ = new NormalEnemyEmitter();
        }

        public void Tick(float deltaTime)
        {
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
            var go = LiteRuntime.Get<AssetSystem>().InstantiateSync("Infinite/Prefab/Player.prefab");
            Player_ = new Player(go, new CircleArea(0.2f));
            Player_.MoveSpeed = 4;
            
            Player_.AddSkill(new Skill1());
            Player_.AddSkill(new Skill2());
            Player_.AddSkill(new Skill3());
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
            enemy.Hp = 1;
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

        public Enemy FindEnemy()
        {
            var minDist = float.MaxValue;
            Enemy target = null;
            
            EnemyList_.Foreach((enemy) =>
            {
                var dist = Vector3.Distance(enemy.GetPosition(), Player_.GetPosition());
                if (dist < minDist)
                {
                    minDist = dist;
                    target = enemy;
                }
            });

            return target;
        }

        public BulletTrack CreateBulletTrack(Vector3 beginPos, Vector3 targetPos, float speed)
        {
            var go = BulletPool_.Alloc();
            go.tag = Const.Tag.Bullet;

            var bullet = new BulletTrack(go, new CircleArea(0.08f));
            bullet.Pool = BulletPool_;
            bullet.SetPosition(beginPos);
            bullet.LifeTime = 3;
            bullet.Angular = MathUtils.AngleByPoint(beginPos, targetPos);
            bullet.AngularAcceleration = 0;
            bullet.AngularAccelerationDelay = 0;
            bullet.Velocity = speed;
            bullet.Acceleration = 0;
            bullet.AccelerationDelay = 0;
            bullet.Damage = 1;
            
            BulletList_.Add(bullet);
            return bullet;
        }

        public BulletCurve CreateBulletCurve(CurveData curve, float speed)
        {
            var go = BulletPool_.Alloc();
            go.tag = Const.Tag.Bullet;

            var bullet = new BulletCurve(go, new CircleArea(0.08f), curve, speed);
            bullet.Pool = BulletPool_;
            bullet.Damage = 1;
            
            BulletList_.Add(bullet);
            return bullet;
        }

        public BulletArea CreateBulletArea(int level, float interval)
        {
            var scale = 1f + (level - 1) * 0.3f;
            
            var go = LiteRuntime.Get<AssetSystem>().InstantiateSync("Infinite/Prefab/Bullet2.prefab");
            go.tag = Const.Tag.Bullet;
            go.transform.localScale = Vector3.one * scale;

            var bullet = new BulletArea(go, new CircleArea(0.64f * scale), interval);
            bullet.Damage = 1;
            
            BulletList_.Add(bullet);
            return bullet;
        }

        public void RemoveBullet(BulletBase bullet)
        {
            if (bullet == null)
            {
                return;
            }

            if (bullet.Pool != null)
            {
                bullet.Pool.Recycle(bullet.Go);
            }
            else
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(bullet.Go);
            }

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