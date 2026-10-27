using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ITick, IDisposeAsync
    {
        private readonly List<ISystem> _addList = new();
        private readonly List<ISystem> _systemList = new();
        private readonly List<ISystem> _ownedSystems = new();
        private readonly Dictionary<System.Type, ISystem> _systemTypeMap = new();

        private SystemCenter()
        {
        }

        public async UniTask DisposeAsync()
        {
            await UnInitializeSystem();
        }

        public void Tick(float deltaTime)
        {
            ProcessAddList();

            foreach (var system in _systemList)
            {
                if (!_ownedSystems.Contains(system))
                {
                    continue;
                }

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

        internal async UniTask<bool> InitializeSystem(CancellationToken ct = default)
        {
            if (_ownedSystems.Count > 0)
            {
                throw new System.InvalidOperationException("Previous systems have not been disposed.");
            }

            var systemList = GetAllSystemList();

            foreach (var assemblyQualifiedName in systemList)
            {
                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                var systemType = System.Type.GetType(assemblyQualifiedName);
                if (systemType == null)
                {
                    throw new System.Exception($"cannot find system class type : {assemblyQualifiedName}");
                }

                if (System.Activator.CreateInstance(systemType) is not ISystem system)
                {
                    throw new System.Exception($"incorrect {typeof(ISystem)} type : {assemblyQualifiedName}");
                }

                _ownedSystems.Add(system);

                LLog.Info("Initialize {0}", assemblyQualifiedName);

                TryInjectSetting(system);

                var result = await system.Initialize();

                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                if (!result)
                {
                    LLog.Error("Initialize {0} failed", assemblyQualifiedName);
                    return false;
                }

                _addList.Add(system);
                _systemTypeMap.Add(system.GetType(), system);
            }

            return !ct.IsCancellationRequested;
        }

        internal async UniTask UnInitializeSystem()
        {
            await LiteRuntime.Instance.WaitForTickExitAsync();

            while (_ownedSystems.Count > 0)
            {
                var index = _ownedSystems.Count - 1;
                var system = _ownedSystems[index];

                await system.DisposeAsync();

                _ownedSystems.RemoveAt(index);

                var systemType = system.GetType();
                if (_systemTypeMap.TryGetValue(systemType, out var registeredSystem) && ReferenceEquals(registeredSystem, system))
                {
                    _systemTypeMap.Remove(systemType);
                }

                await LiteRuntime.Instance.WaitForTickExitAsync();

                _addList.Remove(system);
                _systemList.Remove(system);
            }
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
            systemList.Sort(static (x, y) => LiteConst.InternalSystem[y].CompareTo(LiteConst.InternalSystem[x]));

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
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystemSettingProvider<>));

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
