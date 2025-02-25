using UnityEngine;

namespace LiteBattle.Runtime
{
    public static class LiteMathHelper
    {
        public static float Max(float a, float b)
        {
            return Mathf.Max(a, b);
        }

        public static float Min(float a, float b)
        {
            return Mathf.Min(a, b);
        }
    }
}