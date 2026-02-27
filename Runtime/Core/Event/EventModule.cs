using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class EventModule : IDispose
    {
        private abstract class EventListener
        {
            public abstract void UnRegisterAll(int tag);
            public abstract int GetListenerCount();
#if UNITY_EDITOR
            public abstract void Check();
#endif
        }

        private class EventListener<T> : EventListener where T : IEventData
        {
            private readonly Dictionary<int, List<Action<T>>> _callbackMap = new();

            public void Trigger(T msg)
            {
                if (_callbackMap.Count == 0)
                {
                    return;
                }

                UnityEngine.Pool.ListPool<Action<T>>.Get(out var snapshot);
                foreach (var callbackList in _callbackMap.Values)
                {
                    snapshot.AddRange(callbackList);
                }

                foreach (var callback in snapshot)
                {
                    LiteUtils.SafeInvoke(callback, msg);
                }
                UnityEngine.Pool.ListPool<Action<T>>.Release(snapshot);
            }

            public void Register(int tag, Action<T> callback)
            {
                if (_callbackMap.TryGetValue(tag, out var callbackList))
                {
                    if (callbackList.Contains(callback))
                    {
#if UNITY_EDITOR
                        LLog.Warning("[EventSystem] Duplicate registration: {0}.{1}", callback.Method.ReflectedType?.Name, callback.Method.Name);
#endif
                        return;
                    }
                    
                    callbackList.Add(callback);
                }
                else
                {
                    _callbackMap.Add(tag, new List<Action<T>> { callback });
                }
            }

            public void UnRegister(int tag, Action<T> callback)
            {
                if (_callbackMap.TryGetValue(tag, out var callbackList))
                {
                    callbackList.Remove(callback);

                    if (callbackList.Count == 0)
                    {
                        UnRegisterAll(tag);
                    }
                }
            }

            public override void UnRegisterAll(int tag)
            {
                _callbackMap.Remove(tag);
            }

            public override int GetListenerCount()
            {
                var count = 0;
                foreach (var callbackList in _callbackMap.Values)
                {
                    count += callbackList.Count;
                }
                return count;
            }

#if UNITY_EDITOR
            public override void Check()
            {
                foreach (var (id, callbackList) in _callbackMap)
                {
                    foreach (var callback in callbackList)
                    {
                        LLog.Warning("[EventSystem] {0}-{1}.{2} UnRegister", id, callback.Method.ReflectedType?.Name, callback.Method.Name);
                    }
                }
            }
#endif
        }

        public string Name { get; }

        private readonly int _globalTag;
        private readonly Dictionary<Type, EventListener> _eventMap = new();
        private bool _disposed;

        public EventModule(string name)
        {
            Name = name;
            _globalTag = $"{name}_Global".GetHashCode();
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            
#if UNITY_EDITOR
            foreach (var chunk in _eventMap)
            {
                chunk.Value.Check();
            }
#endif
            _eventMap.Clear();
        }

        public void Send<T>(T msg) where T : IEventData
        {
            if (_eventMap.TryGetValue(typeof(T), out var value))
            {
                if (value is EventListener<T> listener)
                {
                    listener.Trigger(msg);
                }
            }
        }

        public void Send<T>() where T : IEventData, new()
        {
            var msg = new T();
            Send(msg);
        }

        public void Register<T>(Action<T> callback) where T : IEventData
        {
            Register(_globalTag, callback);
        }

        public void Register<T>(int tag, Action<T> callback) where T : IEventData
        {
            if (_disposed)
            {
                return;
            }
            
            EventListener<T> listener = null;
            var eventType = typeof(T);

            if (_eventMap.TryGetValue(eventType, out var value))
            {
                listener = value as EventListener<T>;
            }
            else
            {
                listener = new EventListener<T>();
                _eventMap.Add(eventType, listener);
            }

            listener?.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            UnRegister(_globalTag, callback);
        }

        public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
        {
            if (_eventMap.TryGetValue(typeof(T), out var value))
            {
                if (value is EventListener<T> listener)
                {
                    listener.UnRegister(tag, callback);
                }
            }
        }

        public void UnRegisterAll(int tag)
        {
            foreach (var chunk in _eventMap)
            {
                chunk.Value.UnRegisterAll(tag);
            }
        }

        internal Dictionary<Type, int> GetEventDebugInfo()
        {
            var info = new Dictionary<Type, int>();
            foreach (var (type, listener) in _eventMap)
            {
                info[type] = listener.GetListenerCount();
            }
            return info;
        }
    }
}