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
            private readonly Dictionary<int, Action<T>> _callbackList = new();

            public void Trigger(T msg)
            {
                foreach (var chunk in _callbackList)
                {
                    LiteUtils.SafeInvoke(chunk.Value, msg);
                }
            }

            public void Register(int tag, Action<T> callback)
            {
                if (!_callbackList.TryAdd(tag, callback))
                {
                    _callbackList[tag] += callback;
                }
            }

            public void UnRegister(int tag, Action<T> callback)
            {
                if (_callbackList.ContainsKey(tag))
                {
                    _callbackList[tag] -= callback;

                    if (_callbackList[tag] == null)
                    {
                        UnRegisterAll(tag);
                    }
                }
            }

            public override void UnRegisterAll(int tag)
            {
                _callbackList.Remove(tag);
            }

#if UNITY_EDITOR
            public override void Check()
            {
                foreach (var chunk in _callbackList)
                {
                    if (chunk.Value != null)
                    {
                        var invocationList = chunk.Value.GetInvocationList();
                        foreach (var invocation in invocationList)
                        {
                            LLog.Warning($"{chunk.Key}-{invocation.Method.ReflectedType?.Name} : {invocation.Method.Name} UnRegister");
                        }
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