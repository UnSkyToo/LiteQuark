using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LiteLog : Singleton<LiteLog>
    {
        private const string DefaultTag = "Default";
        
        public void Info(string msg)
        {
            Info(DefaultTag, msg);
        }
        
        public void Info(string tag, string msg)
        {
            Debug.Log(msg);
        }

        public void Warning(string msg)
        {
            Warning(DefaultTag, msg);
        }

        public void Warning(string tag, string msg)
        {
            Debug.LogWarning(msg);
        }

        public void Error(string msg)
        {
            Error(DefaultTag, msg);
        }

        public void Error(string tag, string msg)
        {
            Debug.LogError(msg);
        }
    }

    internal static class LLog
    {
        private const string Tag = "LiteQuark";
        
        internal static void Info(string msg)
        {
            LiteLog.Instance.Info(Tag, msg);
        }

        internal static void Warning(string msg)
        {
            LiteLog.Instance.Warning(Tag, msg);
        }

        internal static void Error(string msg)
        {
            LiteLog.Instance.Error(Tag, msg);
        }
    }
}