using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class EventModule : IDispose
    {
        private abstract class EventListener
        {
            public abstract void UnRegisterAll(int tag);
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
                
                foreach (var callbackList in _callbackMap.Values)
                {
                    foreach (var callback in callbackList)
                    {
                        LiteUtils.SafeInvoke(callback, msg);
                    }
                }
            }

            public void Register(int tag, Action<T> callback)
            {
                if (_callbackMap.TryGetValue(tag, out var callbackList))
                {
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

#if UNITY_EDITOR
            public override void Check()
            {
                foreach (var (id, callbackList) in _callbackMap)
                {
                    foreach (var callback in callbackList)
                    {
                        LLog.Warning($"{id}-{callback.Method.ReflectedType?.Name} : {callback.Method.Name} UnRegister");
                    }
                }
            }
#endif
        }

        public string Name { get; }

        private readonly int _globalTag;
        private readonly Dictionary<string, EventListener> _eventMap = new();

        public EventModule(string name)
        {
            Name = name;
            _globalTag = $"{name}_Global".GetHashCode();
            _eventMap.Clear();
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            foreach (var chunk in _eventMap)
            {
                chunk.Value.Check();
            }
#endif
            _eventMap.Clear();
        }

        private static string GetEventName<T>() where T : IEventData
        {
            return typeof(T).FullName;
        }

        public void Send<T>(T msg) where T : IEventData
        {
            var eventName = GetEventName<T>();
            if (_eventMap.TryGetValue(eventName, out var value))
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
            var eventName = GetEventName<T>();
            EventListener<T> listener = null;

            if (_eventMap.TryGetValue(eventName, out var value))
            {
                listener = value as EventListener<T>;
            }
            else
            {
                listener = new EventListener<T>();
                _eventMap.Add(eventName, listener);
            }

            listener?.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            UnRegister(_globalTag, callback);
        }

        public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
        {
            var eventName = GetEventName<T>();

            if (_eventMap.TryGetValue(eventName, out var value))
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
    }
}