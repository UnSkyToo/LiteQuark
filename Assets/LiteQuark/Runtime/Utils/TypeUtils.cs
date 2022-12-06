using System;

namespace LiteQuark.Runtime
{
    public static class TypeUtils
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
    }
}