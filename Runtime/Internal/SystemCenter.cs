using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class SystemCenter : Singleton<SystemCenter>, ISubstance
    {
        private static readonly Dictionary<System.Type, int> RegisterSystemMap_ = new Dictionary<System.Type, int>();
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
            foreach (var system in SystemList_)
            {
                if (system is ITick tickSys)
                {
                    tickSys.Tick(deltaTime);
                }
            }
        }
        
        internal void InitializeSystem(System.Action<bool> callback)
        {
            SystemList_.Clear();
            SystemTypeMap_.Clear();

            var registerSystemList = new List<System.Type>(RegisterSystemMap_.Keys);
            registerSystemList.Sort((x, y) => RegisterSystemMap_[y].CompareTo(RegisterSystemMap_[x]));

            new AsyncInitializer<ISystem>(registerSystemList, (system, isError) =>
            {
                if (isError)
                {
                    callback?.Invoke(false);
                    return;
                }

                if (system != null)
                {
                    SystemList_.Add(system);
                    SystemTypeMap_.Add(system.GetType(), system);
                }
                else
                {
                    callback?.Invoke(true);
                }
            }).StartInitialize();
        }
        
        internal void UnInitializeSystem()
        {
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
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterSystem()
        {
            RegisterSystemMap_.Clear();
            
            RegisterSystem<LogSystem>(99900);
            RegisterSystem<EventSystem>(99800);
            RegisterSystem<TaskSystem>(99700);
            RegisterSystem<TimerSystem>(99600);
            RegisterSystem<GroupSystem>(99500);
            RegisterSystem<AssetSystem>(99400);
            RegisterSystem<ObjectPoolSystem>(99300);
            RegisterSystem<AudioSystem>(99200);
            RegisterSystem<ActionSystem>(99100);
        }

        /// <summary>
        /// Register LiteQuark runtime module
        /// </summary>
        /// <param name="priority">Sort by priority value from high to low, can't greater than 90000</param>
        public static void RegisterSystem<T>(int priority) where T : ISystem
        {
            var type = typeof(T);
            if (RegisterSystemMap_.ContainsKey(type))
            {
                return;
            }
            
            RegisterSystemMap_.Add(type, priority);
        }
    }
}