using UnityEngine;

namespace InfiniteGame
{
    [CreateAssetMenu(fileName = "curve", menuName = "InfiniteGame/Curve Data", order = 1)]
    public class CurveData : ScriptableObject
    {
        public bool Loop;
        public Vector2[] Points;
    }
}