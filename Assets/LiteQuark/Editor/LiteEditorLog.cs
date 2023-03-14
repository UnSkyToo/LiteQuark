using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class LEditorLog
    {
        internal static void Info(string msg)
        {
            Debug.Log(msg);
        }

        internal static void Warning(string msg)
        {
            Debug.LogWarning(msg);
        }

        internal static void Error(string msg)
        {
            Debug.LogError(msg);
        }
    }
}