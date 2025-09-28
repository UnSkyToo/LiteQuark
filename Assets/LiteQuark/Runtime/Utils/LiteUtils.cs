using System;

namespace LiteQuark.Runtime
{
    public static class LiteUtils
    {
        public static void SafeInvoke(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }
        
        public static void SafeInvoke<T>(Action<T> callback, T arg)
        {
            try
            {
                callback?.Invoke(arg);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }
    }
}