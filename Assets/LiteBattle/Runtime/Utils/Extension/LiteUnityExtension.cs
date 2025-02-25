using System;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public static class LiteUnityExtension
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        public static Vector3 Round(this Vector3 vec3, int digits)
        {
            return new Vector3(
                (float) Math.Round(vec3.x, digits),
                (float) Math.Round(vec3.y, digits),
                (float) Math.Round(vec3.z, digits));
        }
    }
}