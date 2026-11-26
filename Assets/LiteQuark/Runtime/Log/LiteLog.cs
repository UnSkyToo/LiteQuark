using System;
using System.Globalization;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class LLog
    {
        private static bool IsEnabled(LogLevel level)
        {
            var setting = LiteRuntime.Setting;
            return setting == null || LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle ||
                (setting.Log.Levels & level) != 0;
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Info(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Info))
            {
                Debug.LogFormat(format, args);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Info(string msg)
        {
            if (IsEnabled(LogLevel.Info))
            {
                Debug.Log(msg);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Warning(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                Debug.LogWarningFormat(format, args);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Warning(string msg)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                Debug.LogWarning(msg);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Error(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Error))
            {
                Debug.LogErrorFormat(format, args);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Error(string msg)
        {
            if (IsEnabled(LogLevel.Error))
            {
                Debug.LogError(msg);
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Exception(Exception ex, string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Fatal))
            {
                var message = args.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, args);
                Debug.LogException(new Exception(message, ex));
            }
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Exception(Exception ex)
        {
            if (IsEnabled(LogLevel.Fatal))
            {
                Debug.LogException(ex);
            }
        }
    }
}
