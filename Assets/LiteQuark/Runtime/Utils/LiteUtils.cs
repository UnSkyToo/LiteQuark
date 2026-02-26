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
        
        public static void SafeInvoke<T1, T2>(Action<T1, T2> callback, T1 arg1, T2 arg2)
        {
            try
            {
                callback?.Invoke(arg1, arg2);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }
        
        public static void SafeInvoke<T1, T2, T3>(Action<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3)
        {
            try
            {
                callback?.Invoke(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }
    }
}