using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LiteLog : Singleton<LiteLog>
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
}