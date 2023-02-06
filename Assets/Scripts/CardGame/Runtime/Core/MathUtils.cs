using UnityEngine;

namespace LiteCard
{
    public static class MathUtils
    {
        public static float Angle(Vector2 from, Vector2 to)
        {
            var cross = Vector3.Cross(from, to);
            var angle = Vector2.Angle(from, to);
            return cross.z > 0 ? -angle : angle;
        }
    }
}