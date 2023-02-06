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
        
        public static float Angle(Vector2 from, Vector2 to)
        {
            var cross = Vector3.Cross(from, to);
            var angle = Vector2.Angle(from, to);
            return cross.z > 0 ? -angle : angle;
        }
    }
}