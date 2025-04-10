using System;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public abstract class Singleton<T> where T :  class
    {
        private static readonly Lazy<T> Instance_ = new Lazy<T>(CreateInstance);

        public static T Instance => Instance_.Value;

        protected Singleton()
        {
        }

        static Singleton()
        {
        }

        private static T CreateInstance()
        {
            try
            {
                return Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic, null, null, null) as T;
            }
            catch (MissingMethodException ex)
            {
                throw new Exception($"(单例模式下，未找到构造函数或者构造函数为public)\n{ex.Message}");
            }
        }
    }
}