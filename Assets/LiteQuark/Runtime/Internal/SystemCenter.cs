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
                foreach (var assemblyQualifiedName in GetAllSystemList())
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
                    TryInjectSetting(unpublished);
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
