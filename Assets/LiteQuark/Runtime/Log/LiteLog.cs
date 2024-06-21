using System;

namespace LiteQuark.Runtime
{
    internal static class LLog
    {
        private static ILog Log_ = null;
        
        private static ILog GetLog()
        {
            if (Log_ == null)
            {
                Log_ = LiteRuntime.Log?.GetLogger(LiteConst.Tag);
            }

            return Log_;
        }
        
        internal static void Info(string msg)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.Log(msg);
            }
            else
            {
                log.Info(msg);
            }
        }

        internal static void Warning(string msg)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogWarning(msg);
            }
            else
            {
                log.Warn(msg);
            }
        }

        internal static void Error(string msg)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogError(msg);
            }
            else
            {
                log.Error(msg);
            }
        }

        internal static void Exception(Exception ex)
        {
            Error($"{ex.Message}\n{ex.StackTrace}");
        }
    }
}