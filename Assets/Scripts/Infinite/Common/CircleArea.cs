using UnityEngine;

namespace InfiniteGame
{
    public struct CircleArea
    {
        public static readonly CircleArea None = new CircleArea(-10000, -10000, 0);
        
        public float X;
        public float Y;
        public float Radius;

        public CircleArea(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public CircleArea(float radius)
        {
            X = 0;
            Y = 0;
            Radius = radius;
        }

        public void Move(Vector3 position)
        {
            X = position.x;
            Y = position.y;
        }

        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        public bool IsOverlap(CircleArea other)
        {
            var x = X - other.X;
            var y = Y - other.Y;
            var dist = Mathf.Sqrt(x * x + y * y);
            return dist <= (Radius + other.Radius);
        }
    }
}