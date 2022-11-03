using System;

namespace LiteQuark.Runtime
{
    public static class TypeHelper
    {
        public static Type GetTypeWithLogicClassName(string logicClassName)
        {
            var chunks = logicClassName.Split('|');
            if (chunks.Length != 2)
            {
                return null;
            }

            return GetTypeWithAssembly(chunks[1], chunks[0]);
        }
        
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