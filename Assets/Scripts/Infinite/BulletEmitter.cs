using LiteQuark.Runtime;

namespace InfiniteGame
{
    public interface IBulletEmitter
    {
        void Fire(Enemy target);
    }

    public sealed class BulletSerialEmitter : IBulletEmitter
    {
        public Player Player { get; }
        public float Interval { get; }
        
        public BulletSerialEmitter(Player player, float interval)
        {
            Player = player;
            Interval = interval;
        }
        
        public void Fire(Enemy target)
        {
            if (target == null)
            {
                return;
            }
            
            var targetPos = target.transform.localPosition;
            
            LiteRuntime.Get<TimerSystem>().AddTimer(Interval, () =>
            {
                BulletManager.Instance.CreateBullet(Player.transform.localPosition, targetPos, 20);
            }, Player.Level);
        }
    }
}