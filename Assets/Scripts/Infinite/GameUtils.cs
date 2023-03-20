using UnityEngine;

namespace InfiniteGame
{
    public static class GameUtils
    {
        public static Vector3 RandomVec()
        {
            var angle = Random.Range(0, 360);
            return Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
        }

        public static Vector3 RandomPosition(float distance)
        {
            var dir = RandomVec();
            return dir.normalized * distance;
        }
    }
}