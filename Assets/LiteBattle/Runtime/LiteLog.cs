using UnityEngine;

namespace LiteBattle.Runtime
{
    public static class LiteLog
    {
        public static void Info(string msg)
        {
            Debug.Log(msg);
        }

        public static void Warning(string msg)
        {
            Debug.LogWarning(msg);
        }

        public static void Error(string msg)
        {
            Debug.LogError(msg);
        }
    }
}