using System;

namespace LiteQuark.Runtime
{
    public static class LLog
    {
        private static ILog _log = null;
        
        private static ILog GetLog()
        {
            if (_log == null)
            {
                _log = LiteRuntime.Log?.GetLogger(LiteConst.Tag);
            }

            return _log;
        }

        public static void Info(string format, params object[] args)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogFormat(format, args);
            }
            else
            {
                log.Info(format, args);
            }
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
        
        public static void Warning(string format, params object[] args)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogWarningFormat(format, args);
            }
            else
            {
                log.Warn(format, args);
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
        
        public static void Error(string format, params object[] args)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogErrorFormat(format, args);
            }
            else
            {
                log.Error(format, args);
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
        
        public static void Exception(Exception ex, string format, params object[] args)
        {
            var log = GetLog();
            if (log == null)
            {
                UnityEngine.Debug.LogErrorFormat(format, args);
                UnityEngine.Debug.LogException(ex);
            }
            else
            {
                log.Fatal(ex, format, args);
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
                log.Fatal(ex, ex.Message);
            }
        }
    }
}