﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ISubstance
    {
        private readonly List<ISystem> AddList_ = new List<ISystem>();
        private readonly List<ISystem> SystemList_ = new List<ISystem>();
        private readonly Dictionary<System.Type, ISystem> SystemTypeMap_ = new Dictionary<System.Type, ISystem>();
        
        private SystemCenter()
        {
        }

        public void Dispose()
        {
            UnInitializeSystem();
        }
        
        public void Tick(float deltaTime)
        {
            ProcessAddList();
            
            foreach (var system in SystemList_)
            {
                if (system is ITick tickSys)
                {
                    tickSys.Tick(deltaTime);
                }
            }
        }

        private void ProcessAddList()
        {
            if (AddList_.Count > 0)
            {
                foreach (var system in AddList_)
                {
                    SystemList_.Add(system);
                }

                AddList_.Clear();
            }
        }

        internal async Task<bool> InitializeSystem()
        {
            AddList_.Clear();
            SystemList_.Clear();
            SystemTypeMap_.Clear();

            var systemList = GetAllSystemList();
            
            foreach (var assemblyQualifiedName in systemList)
            {
                var systemType = System.Type.GetType(assemblyQualifiedName);
                if (systemType == null)
                {
                    throw new System.Exception($"can't not find system class type : {assemblyQualifiedName}");
                }
                
                if (System.Activator.CreateInstance(systemType) is not ISystem system)
                {
                    throw new System.Exception($"incorrect {typeof(ISystem)} type : {assemblyQualifiedName}");
                }
                
                LLog.Info($"Initialize {assemblyQualifiedName}");
                
                var result = await system.Initialize();
                if (result)
                {
                    AddList_.Add(system);
                    SystemTypeMap_.Add(system.GetType(), system);
                }
                else
                {
                    LLog.Error($"Initialize {assemblyQualifiedName} failed");
                    return false;
                }
            }

            return true;
        }
        
        internal void UnInitializeSystem()
        {
            ProcessAddList();
            
            for (var index = SystemList_.Count - 1; index >= 0; --index)
            {
                SystemList_[index].Dispose();
            }
            
            SystemList_.Clear();
            SystemTypeMap_.Clear();
        }
        
        public T GetSystem<T>() where T : ISystem
        {
            if (SystemTypeMap_.TryGetValue(typeof(T), out var system))
            {
                return (T) system;
            }

            return default;
        }

        private List<string> GetAllSystemList()
        {
            var systemList = new List<string>(LiteConst.InternalSystem.Keys);
            systemList.Sort((x, y) => LiteConst.InternalSystem[y].CompareTo(LiteConst.InternalSystem[x]));

            foreach (var systemEntry in LiteRuntime.Setting.SystemList)
            {
                if (systemEntry.Disabled)
                {
                    continue;
                }

                var assemblyQualifiedName = systemEntry.AssemblyQualifiedName;
                if (string.IsNullOrWhiteSpace(assemblyQualifiedName) || systemList.Contains(assemblyQualifiedName))
                {
                    continue;
                }
                
                systemList.Add(assemblyQualifiedName);
            }

            return systemList;
        }
    }
}