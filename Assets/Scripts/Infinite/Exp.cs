using UnityEngine;

namespace InfiniteGame
{
    public sealed class Exp : MonoBehaviour
    {
        public int Value;

        public void OnPlayerCollision()
        {
            Value = 0;
            BattleManager.Instance.RemoveExp(this);
        }
    }
}