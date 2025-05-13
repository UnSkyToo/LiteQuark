using System;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class MathUtils
    {
        private static readonly System.Random Rand = new System.Random((int)DateTime.Now.Ticks);
        
        public static bool Approximately(double a, double b)
        {
            return Math.Abs(a - b) < 0.000000001d;
        }
        
        public static int RandInt(int min, int max)
        {
            return Rand.Next(min, max);
        }

        public static float RandFloat()
        {
            return (float)Rand.NextDouble();
        }

        public static float ClampMinTime(float time)
        {
            return Mathf.Max(time, LiteConst.MinIntervalTime);
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
        
        public static Vector3 Round(this Vector3 vec3, int digits)
        {
            return new Vector3(
                (float) Math.Round(vec3.x, digits),
                (float) Math.Round(vec3.y, digits),
                (float) Math.Round(vec3.z, digits));
        }

        public static Vector3[] VectorListAdd(Vector3[] list, Vector3 value)
        {
            if (list == null || list.Length == 0)
            {
                return list;
            }
            
            var result = new Vector3[list.Length];
            for (var i = 0; i < list.Length; ++i)
            {
                result[i] = list[i] + value;
            }
            return result;
        }
        
        public static float VectorListLength(Vector3[] list)
        {
            if (list == null || list.Length == 0)
            {
                return 0f;
            }

            if (list.Length == 1)
            {
                return Vector3.Distance(Vector3.zero, list[0]);
            }
            
            var length = 0f;
            
            for (var i = 0; i < list.Length - 1; ++i)
            {
                length += Vector3.Distance(list[i], list[i + 1]);
            }

            return length;
        }
    }
}