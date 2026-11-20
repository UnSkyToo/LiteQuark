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

        private static bool FallbackEnabled(LogLevel level)
        {
            var setting = LiteRuntime.Setting;
            return setting == null || LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle || (setting.Log.Levels & level) == level;
        }

        internal static void Reset()
        {
            _log = null;
        }

        public static void Info(string format, params object[] args)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Info(format, args);
            }
            else if (FallbackEnabled(LogLevel.Info))
            {
                UnityEngine.Debug.LogFormat(format, args);
            }
        }

        public static void Info(string msg)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Info(msg);
            }
            else if (FallbackEnabled(LogLevel.Info))
            {
                UnityEngine.Debug.Log(msg);
            }
        }

        public static void Warning(string format, params object[] args)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Warn(format, args);
            }
            else if (FallbackEnabled(LogLevel.Warn))
            {
                UnityEngine.Debug.LogWarningFormat(format, args);
            }
        }

        public static void Warning(string msg)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Warn(msg);
            }
            else if (FallbackEnabled(LogLevel.Warn))
            {
                UnityEngine.Debug.LogWarning(msg);
            }
        }

        public static void Error(string format, params object[] args)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Error(format, args);
            }
            else if (FallbackEnabled(LogLevel.Error))
            {
                UnityEngine.Debug.LogErrorFormat(format, args);
            }
        }

        public static void Error(string msg)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Error(msg);
            }
            else if (FallbackEnabled(LogLevel.Error))
            {
                UnityEngine.Debug.LogError(msg);
            }
        }

        public static void Exception(Exception ex, string format, params object[] args)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Fatal(ex, format, args);
            }
            else if (FallbackEnabled(LogLevel.Fatal))
            {
                UnityEngine.Debug.LogErrorFormat(format, args);
                UnityEngine.Debug.LogException(ex);
            }
        }

        public static void Exception(Exception ex)
        {
            var log = GetLog();
            if (log != null)
            {
                log.Fatal(ex, ex.Message);
            }
            else if (FallbackEnabled(LogLevel.Fatal))
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
