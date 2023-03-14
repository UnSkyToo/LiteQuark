using UnityEngine;

namespace Platform
{
    public struct AABB
    {
        public Vector2 Center { get; set; }
        public Vector2 HalfSize { get; set; }
        public Vector2 Offset { get; set; }

        public AABB(float x, float y, float halfW, float halfH, float offsetX, float offsetY)
            : this(new Vector2(x, y), new Vector2(halfW, halfH), new Vector2(offsetX, offsetY))
        {
        }
        
        public AABB(Vector2 center, Vector2 halfSize, Vector2 offset)
        {
            Center = center;
            HalfSize = halfSize;
            Offset = offset;
        }

        public bool Overlaps(AABB other)
        {
            if (Mathf.Abs((Center.x + Offset.x) - (other.Center.x + other.Offset.x)) > HalfSize.x + other.HalfSize.x)
            {
                return false;
            }

            if (Mathf.Abs((Center.y + Offset.y) - (other.Center.y + other.Center.y)) > HalfSize.y + other.HalfSize.y)
            {
                return false;
            }
            
            return true;
        }
    }
}