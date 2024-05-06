using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class MathUtils
    {
        private static readonly System.Random Rand_ = new System.Random((int)DateTime.Now.Ticks);
        
        public static bool Approximately(double a, double b)
        {
            return Math.Abs(a - b) < 0.000000001d;
        }
        
        public static int RandInt(int min, int max)
        {
            return Rand_.Next(min, max);
        }

        public static float RandFloat()
        {
            return (float)Rand_.NextDouble();
        }
        
        public static float AngleByVector(Vector2 from, Vector2 to)
        {
            var cross = Vector3.Cross(from, to);
            var angle = Vector2.Angle(from, to);
            return cross.z > 0 ? 360f - angle : angle;
        }

        public static float AngleByPoint(Vector2 begin, Vector2 end)
        {
            var from = Vector2.up;
            var to = (end - begin).normalized;
            var angle = AngleByVector(from, to);
            return angle;
        }
        
        /// <summary>Returns a Vector3 with z = 0</summary>
        public static Vector3 Vector3FromAngle(float degrees, float magnitude)
        {
            var f = degrees * (Mathf.PI / 180f);
            return new Vector3(magnitude * Mathf.Cos(f), magnitude * Mathf.Sin(f), 0.0f);
        }
        
        /// <summary>Returns the 2D angle between two vectors</summary>
        public static float Angle2D(Vector3 from, Vector3 to)
        {
            to -= from;
            
            var angle = Vector2.Angle(Vector2.right, to);
            if (Vector3.Cross(Vector2.right, to).z > 0.0f)
            {
                angle = 360f - angle;
            }
            
            return angle * -1f;
        }

        public static Vector2 Round(this Vector2 vec)
        {
            return new Vector2(Mathf.Round(vec.x), Mathf.Round(vec.y));
        }

        public static Vector3 Round(this Vector3 vec)
        {
            return new Vector3(Mathf.Round(vec.x), Mathf.Round(vec.y), Mathf.Round(vec.z));
        }
    }
}