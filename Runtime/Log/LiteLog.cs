using System;

namespace LiteQuark.Runtime
{
    public static class LLog
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
        
        public static void Info(string msg)
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

        public static void Warning(string msg)
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

        public static void Error(string msg)
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

        public static void Exception(Exception ex)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogException(ex);
            }
            else
            {
                log.Fatal(ex.Message, ex);
            }
        }
    }
}