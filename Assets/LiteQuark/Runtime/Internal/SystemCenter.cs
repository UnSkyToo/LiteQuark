using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ITick, ILateTick, IDisposeAsync
    {
        private readonly List<ISystem> _systemList = new();
        private readonly Dictionary<System.Type, ISystem> _systemTypeMap = new();

        private SystemCenter()
        {
        }

        public void Tick(float deltaTime)
        {
            // Completing an initialization inside Tick can append newly published systems.
            var count = _systemList.Count;
            for (var i = 0; i < count && i < _systemList.Count; i++)
            {
                if (LiteRuntime.Instance.State is LiteRuntime.RuntimeState.Error or LiteRuntime.RuntimeState.ShutdownFailed)
                {
                    break;
                }

                if (_systemList[i] is ITick tickSys)
                {
                    tickSys.Tick(deltaTime);
                }
            }
        }

        public void LateTick(float deltaTime)
        {
            var runtime = LiteRuntime.Instance;
            for (var i = 0; i < _systemList.Count && runtime.CanLateTick; i++)
            {
                if (_systemList[i] is ILateTick system)
                {
                    system.LateTick(deltaTime);
                }
            }
        }

        internal async UniTask<bool> InitializeSystem(CancellationToken ct = default)
        {
            ISystem unpublished = null;
            try
            {
                foreach (var (assemblyQualifiedName, settings) in GetAllSystems())
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

                    unpublished = System.Activator.CreateInstance(systemType) as ISystem;
                    if (unpublished == null)
                    {
                        throw new System.Exception($"incorrect {typeof(ISystem)} type : {assemblyQualifiedName}");
                    }

                    LLog.Info("Initialize {0}", assemblyQualifiedName);
                    TryInjectSetting(unpublished, settings);
                    var initialized = await unpublished.Initialize();
                    if (ct.IsCancellationRequested)
                    {
                        return false;
                    }
                    if (!initialized)
                    {
                        LLog.Error("Initialize {0} failed", assemblyQualifiedName);
                        return false;
                    }

                    _systemTypeMap.Add(systemType, unpublished);
                    _systemList.Add(unpublished);
                    unpublished = null;
                }

                return !ct.IsCancellationRequested;
            }
            catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
            catch (System.Exception ex)
            {
                LLog.Exception(ex);
                return false;
            }
            finally
            {
                if (unpublished != null)
                {
                    await unpublished.DisposeAsync();
                }
            }
        }

        public async UniTask DisposeAsync()
        {
            while (_systemList.Count > 0)
            {
                var index = _systemList.Count - 1;
                var system = _systemList[index];
                _systemList.RemoveAt(index);
                await LiteRuntime.FinishAtFrameBoundary(DisposeSystem(system));
            }
        }

        private async UniTask DisposeSystem(ISystem system)
        {
            await system.DisposeAsync();
            _systemTypeMap.Remove(system.GetType());
        }

        public T GetSystem<T>() where T : ISystem
        {
            if (_systemTypeMap.TryGetValue(typeof(T), out var system))
            {
                return (T) system;
            }

            return default;
        }

        private static List<(string, ISystemSetting)> GetAllSystems()
        {
            var seen = new HashSet<string>(LiteConst.InternalSystem.Keys);
            var systems = new List<(string, ISystemSetting)>();
            foreach (var builtin in LiteConst.InternalSystem.OrderByDescending(static pair => pair.Value))
            {
                systems.Add((builtin.Key, null));
            }

            foreach (var systemEntry in LiteRuntime.Setting.SystemList)
            {
                if (systemEntry == null || systemEntry.Disabled || string.IsNullOrWhiteSpace(systemEntry.AssemblyQualifiedName))
                {
                    continue;
                }
                if (seen.Add(systemEntry.AssemblyQualifiedName))
                {
                    systems.Add((systemEntry.AssemblyQualifiedName, systemEntry.Setting));
                }
            }
            return systems;
        }

        private static void TryInjectSetting(ISystem system, ISystemSetting setting)
        {
            var systemType = system.GetType();
            var settingInterface = LiteSystemEntry.GetProviderInterface(systemType);
            if (settingInterface == null)
            {
                return;
            }

            var settingType = settingInterface.GetGenericArguments()[0];
            if (setting == null)
            {
                LLog.Warning("Setting not found for {0}, using default", systemType.Name);
                setting = (ISystemSetting)System.Activator.CreateInstance(settingType);
            }
            else if (!settingType.IsInstanceOfType(setting))
            {
                throw new System.InvalidOperationException($"{systemType.Name} requires {settingType.Name}, got {setting.GetType().Name}.");
            }
            var property = settingInterface.GetProperty("Setting");
            property?.SetValue(system, setting);
        }
    }
}
