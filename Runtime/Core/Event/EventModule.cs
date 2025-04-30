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
            private readonly Dictionary<int, Action<T>> CallbackList_ = new();

            public void Trigger(T msg)
            {
                foreach (var chunk in CallbackList_)
                {
                    chunk.Value?.Invoke(msg);
                }
            }

            public void Register(int tag, Action<T> callback)
            {
                if (!CallbackList_.TryAdd(tag, callback))
                {
                    CallbackList_[tag] += callback;
                }
            }

            public void UnRegister(int tag, Action<T> callback)
            {
                if (CallbackList_.ContainsKey(tag))
                {
                    CallbackList_[tag] -= callback;

                    if (CallbackList_[tag] == null)
                    {
                        UnRegisterAll(tag);
                    }
                }
            }

            public override void UnRegisterAll(int tag)
            {
                CallbackList_.Remove(tag);
            }

#if UNITY_EDITOR
            public override void Check()
            {
                foreach (var chunk in CallbackList_)
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

        private readonly int GlobalTag_;
        private readonly Dictionary<string, EventListener> EventMap_ = new();

        public EventModule(string name)
        {
            Name = name;
            GlobalTag_ = $"{name}_Global".GetHashCode();
            EventMap_.Clear();
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            foreach (var chunk in EventMap_)
            {
                chunk.Value.Check();
            }
#endif
            EventMap_.Clear();
        }

        private static string GetEventName<T>() where T : IEventData
        {
            return typeof(T).FullName;
        }

        public void Send<T>(T msg) where T : IEventData
        {
            var eventName = GetEventName<T>();
            if (EventMap_.TryGetValue(eventName, out var value))
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
            Register(GlobalTag_, callback);
        }

        public void Register<T>(int tag, Action<T> callback) where T : IEventData
        {
            var eventName = GetEventName<T>();
            EventListener<T> listener = null;

            if (EventMap_.TryGetValue(eventName, out var value))
            {
                listener = value as EventListener<T>;
            }
            else
            {
                listener = new EventListener<T>();
                EventMap_.Add(eventName, listener);
            }

            listener?.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            UnRegister(GlobalTag_, callback);
        }

        public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
        {
            var eventName = GetEventName<T>();

            if (EventMap_.TryGetValue(eventName, out var value))
            {
                if (value is EventListener<T> listener)
                {
                    listener.UnRegister(tag, callback);
                }
            }
        }

        public void UnRegisterAll(int tag)
        {
            foreach (var chunk in EventMap_)
            {
                chunk.Value.UnRegisterAll(tag);
            }
        }
    }
}