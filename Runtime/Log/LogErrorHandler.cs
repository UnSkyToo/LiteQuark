using System;

namespace LiteQuark.Runtime
{
    internal static class LogErrorHandler
    {
        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public static void Error(string message, Exception ex)
        {
            Error($"{message}\n{ex}");
        }
    }
}