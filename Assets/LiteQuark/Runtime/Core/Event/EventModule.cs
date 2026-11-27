using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class EventModule : IDispose
    {
        private abstract class EventListener
        {
            public abstract void RemoveScope(EventSubscriptionScope scope);
            public abstract int GetListenerCount();
#if UNITY_EDITOR
            public abstract void Check(string moduleName);
#endif
        }

        private class EventListener<T> : EventListener where T : IEventData
        {
            private readonly Dictionary<EventSubscriptionScope, List<Action<T>>> _callbackMap = new();

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

            public void Register(EventSubscriptionScope scope, Action<T> callback)
            {
                if (_callbackMap.TryGetValue(scope, out var callbackList))
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
                    _callbackMap.Add(scope, new List<Action<T>> { callback });
                }
            }

            public void UnRegister(EventSubscriptionScope scope, Action<T> callback)
            {
                if (_callbackMap.TryGetValue(scope, out var callbackList))
                {
                    callbackList.Remove(callback);

                    if (callbackList.Count == 0)
                    {
                        RemoveScope(scope);
                    }
                }
            }

            public override void RemoveScope(EventSubscriptionScope scope)
            {
                _callbackMap.Remove(scope);
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
            public override void Check(string moduleName)
            {
                foreach (var callbackList in _callbackMap.Values)
                {
                    foreach (var callback in callbackList)
                    {
                        LLog.Warning("[EventSystem] {0}-{1}.{2} UnRegister", moduleName, callback.Method.ReflectedType?.Name, callback.Method.Name);
                    }
                }
            }
#endif
        }

        public string Name { get; }

        private readonly EventSubscriptionScope _defaultScope;
        private readonly Dictionary<Type, EventListener> _eventMap = new();
        private bool _disposed;

        public EventModule(string name)
        {
            Name = name;
            _defaultScope = new EventSubscriptionScope(this);
            _disposed = false;
        }

        public EventSubscriptionScope CreateSubscriptionScope()
        {
            return new EventSubscriptionScope(this);
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
                chunk.Value.Check(Name);
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
            Register(_defaultScope, callback);
        }

        internal void Register<T>(EventSubscriptionScope scope, Action<T> callback) where T : IEventData
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

            listener?.Register(scope, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            UnRegister(_defaultScope, callback);
        }

        internal void UnRegister<T>(EventSubscriptionScope scope, Action<T> callback) where T : IEventData
        {
            if (_eventMap.TryGetValue(typeof(T), out var value))
            {
                if (value is EventListener<T> listener)
                {
                    listener.UnRegister(scope, callback);
                }
            }
        }

        internal void RemoveScope(Type eventType, EventSubscriptionScope scope)
        {
            if (_eventMap.TryGetValue(eventType, out var listener))
            {
                listener.RemoveScope(scope);
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
