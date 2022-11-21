using System;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class TypeHelper
    {
        public static Type GetTypeWithAssembly(string assemblyFullName, string typeName)
        {
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblyList)
            {
                if (assembly.FullName == assemblyFullName)
                {
                    return assembly.GetType(typeName);
                }
            }

            return null;
        }

        public static MethodInfo GetMethod(Type type, string methodName, int paramCount, BindingFlags flags)
        {
            var methods = type.GetMethods(flags);

            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    if (method.GetParameters().Length == paramCount)
                    {
                        return method;
                    }
                }
            }

            return null;
        }
    }
}