namespace LiteQuark.Runtime
{
    internal static class LLog
    {
        private const string Tag = "LiteQuark";
        private static ILog Log_ = null;
        
        private static ILog GetLog()
        {
            if (Log_ == null)
            {
                Log_ = LiteRuntime.Get<LogSystem>()?.GetLogger(Tag);
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
    }
}