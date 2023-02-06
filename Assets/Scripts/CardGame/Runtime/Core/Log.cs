using LiteQuark.Runtime;

namespace LiteCard
{
    public static class Log
    {
        public static void Info(string msg)
        {
            LiteRuntime.Get<LogSystem>().Info(msg);
        }

        public static void Warning(string msg)
        {
            LiteRuntime.Get<LogSystem>().Warn(msg);
        }
        
        public static void Error(string msg)
        {
            LiteRuntime.Get<LogSystem>().Error(msg);
        }

        public static void Fatal(string msg)
        {
            LiteRuntime.Get<LogSystem>().Fatal(msg);
        }
    }
}