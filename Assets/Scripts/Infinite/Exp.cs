using UnityEngine;

namespace InfiniteGame
{
    public sealed class Exp : BattleEntity
    {
        public int Value;

        public Exp(GameObject go, CircleArea circle)
            : base(go, circle)
        {
        }

        public override void Tick(float deltaTime)
        {
        }
        
        public void OnPlayerCollision()
        {
            Value = 0;
            BattleManager.Instance.RemoveExp(this);
        }
    }
}