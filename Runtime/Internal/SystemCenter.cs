using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ISubstance
    {
        private readonly List<ISystem> _addList = new List<ISystem>();
        private readonly List<ISystem> _systemList = new List<ISystem>();
        private readonly Dictionary<System.Type, ISystem> _systemTypeMap = new Dictionary<System.Type, ISystem>();
        
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
            
            foreach (var system in _systemList)
            {
                if (system is ITick tickSys)
                {
                    tickSys.Tick(deltaTime);
                }
            }
        }

        private void ProcessAddList()
        {
            if (_addList.Count > 0)
            {
                foreach (var system in _addList)
                {
                    _systemList.Add(system);
                }

                _addList.Clear();
            }
        }

        internal async UniTask<bool> InitializeSystem()
        {
            _addList.Clear();
            _systemList.Clear();
            _systemTypeMap.Clear();

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
                
                LLog.Info("Initialize {0}", assemblyQualifiedName);
                
                TryInjectSetting(system);
                
                var result = await system.Initialize();
                if (result)
                {
                    _addList.Add(system);
                    _systemTypeMap.Add(system.GetType(), system);
                }
                else
                {
                    LLog.Error("Initialize {0} failed", assemblyQualifiedName);
                    return false;
                }
            }

            return true;
        }
        
        internal void UnInitializeSystem()
        {
            ProcessAddList();
            
            for (var index = _systemList.Count - 1; index >= 0; --index)
            {
                _systemList[index].Dispose();
            }
            
            _systemList.Clear();
            _systemTypeMap.Clear();
        }
        
        public T GetSystem<T>() where T : ISystem
        {
            if (_systemTypeMap.TryGetValue(typeof(T), out var system))
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
        
        private void TryInjectSetting(ISystem system)
        {
            var systemType = system.GetType();
            
            var settingInterface = systemType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystemSettingProvider<>));

            if (settingInterface == null)
            {
                return;
            }
            
            var settingType = settingInterface.GetGenericArguments()[0];
            var setting = LiteRuntime.Setting.GetSetting(settingType);
            var property = settingInterface.GetProperty("Setting");
            property?.SetValue(system, setting);
        }
    }
}