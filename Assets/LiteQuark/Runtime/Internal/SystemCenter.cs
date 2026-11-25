using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ITick, ILateTick
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
                if (LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Error)
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
            async UniTask<bool> InitializeAndPublish(ISystem system, string registeredName = null, ISystemSetting setting = null)
            {
                unpublished = system;
                if (ct.IsCancellationRequested) return false;
                var type = system.GetType();
                var name = registeredName ?? type.AssemblyQualifiedName;
                LLog.Info("Initialize {0}", name);
                if (registeredName != null)
                {
                    TryInjectSetting(system, setting);
                }
                var initialized = await system.Initialize(ct);
                if (ct.IsCancellationRequested) return false;
                if (!initialized)
                {
                    LLog.Error("Initialize {0} failed", name);
                    return false;
                }

                _systemTypeMap.Add(type, system);
                _systemList.Add(system);
                unpublished = null;
                return true;
            }

            try
            {
                if (ct.IsCancellationRequested) return false;
                // Fix the registrations for this startup before any system can yield or edit the list.
                var extensions = LiteRuntime.Setting.SystemList
                    .Where(entry => entry != null && !entry.Disabled && !string.IsNullOrWhiteSpace(entry.AssemblyQualifiedName))
                    .Select(entry => (entry.AssemblyQualifiedName, entry.Setting)).ToArray();

                foreach (var system in CreateBuiltInSystems())
                {
                    if (!await InitializeAndPublish(system)) return false;
                }

                foreach (var (assemblyQualifiedName, setting) in extensions)
                {
                    var systemType = System.Type.GetType(assemblyQualifiedName);
                    if (systemType == null)
                    {
                        throw new System.Exception($"cannot find system class type : {assemblyQualifiedName}");
                    }

                    if (_systemTypeMap.ContainsKey(systemType)) continue;
                    if (System.Activator.CreateInstance(systemType) is not ISystem system)
                    {
                        throw new System.Exception($"incorrect {typeof(ISystem)} type : {assemblyQualifiedName}");
                    }

                    if (!await InitializeAndPublish(system, assemblyQualifiedName, setting)) return false;
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
                    await LiteRuntime.TryDisposeAsync(unpublished);
                }
            }
        }

        internal async UniTask<bool> DisposeAsync()
        {
            var success = true;
            while (_systemList.Count > 0)
            {
                var index = _systemList.Count - 1;
                var system = _systemList[index];
                _systemList.RemoveAt(index);
                success &= await LiteRuntime.FinishAtFrameBoundary(DisposeSystem(system));
            }
            return success;
        }

        private async UniTask<bool> DisposeSystem(ISystem system)
        {
            var success = await LiteRuntime.TryDisposeAsync(system);
            _systemTypeMap.Remove(system.GetType());
            return success;
        }

        public T GetSystem<T>() where T : ISystem
        {
            if (_systemTypeMap.TryGetValue(typeof(T), out var system))
            {
                return (T) system;
            }

            return default;
        }

        private static IEnumerable<ISystem> CreateBuiltInSystems()
        {
            yield return new LogSystem();
            yield return new EventSystem();
            yield return new TimerSystem();
            yield return new TaskSystem();
            yield return new AssetSystem();
            yield return new ObjectPoolSystem();
            yield return new ActionSystem();
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
