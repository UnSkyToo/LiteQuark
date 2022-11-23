using System;

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
                return Activator.CreateInstance<T>();
            }
            catch (MissingMethodException ex)
            {
                throw ex;
            }
        }
    }
}