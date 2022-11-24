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
                Log_ = LiteRuntime.GetLogSystem().GetLogger(Tag);
            }

            return Log_;
        }
        
        internal static void Info(string msg)
        {
            GetLog().Info(msg);
        }

        internal static void Warning(string msg)
        {
            GetLog().Warn(msg);
        }

        internal static void Error(string msg)
        {
            GetLog().Error(msg);
        }
    }
}