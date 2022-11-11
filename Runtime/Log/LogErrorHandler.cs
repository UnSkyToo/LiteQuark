using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    public static class LogErrorHandler
    {
        public static void Error(string message)
        {
            // Debug.Write(message);
            // Trace.Write(message);
            // UnityEngine.Debug.LogError(message);
        }

        public static void Error(string message, Exception ex)
        {
        }
    }
}