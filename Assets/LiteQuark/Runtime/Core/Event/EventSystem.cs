using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class EventSystem : ISystem
    {
        private abstract class EventListener
        {
            public abstract void UnRegisterAll(int tag);
#if UNITY_EDITOR
            public abstract void Check();
#endif
        }

        private class EventListenerImpl<T> : EventListener where T : BaseEvent
        {
            private readonly Dictionary<int, Action<T>> CallbackList_ = new Dictionary<int, Action<T>>();

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
                        var callbackList = chunk.Value.GetInvocationList();

                        foreach (var callback in callbackList)
                        {
                            LLog.Warning($"{chunk.Key}-{callback.Method.ReflectedType?.Name} : {callback.Method.Name} UnRegister");
                        }
                    }
                }
            }
#endif
        }

        private readonly int GlobalTag_ = 0;
        private readonly Dictionary<string, EventListener> EventList_ = new Dictionary<string, EventListener>();

        public EventSystem()
        {
            EventList_.Clear();
            GlobalTag_ = "Global".GetHashCode();
        }
        
        public void Initialize(Action<bool> callback)
        {
            callback?.Invoke(true);
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            foreach (var current in EventList_)
            {
                current.Value.Check();
            }
#endif

            //OnSend = null;
        }

        private string GetEventName<T>()
        {
            return typeof(T).Name;
        }

        public void Send<T>(T msg) where T : BaseEvent
        {
            if (EventList_.TryGetValue(msg.EventName, out var value))
            {
                if (value is EventListenerImpl<T> eventListener)
                {
                    eventListener.Trigger(msg);
                }
                //OnSend?.Invoke(Event);
            }
        }

        public void Send<T>() where T : BaseEvent, new()
        {
            var msg = new T();
            Send(msg);
        }

        public void Register<T>(Action<T> callback) where T : BaseEvent
        {
            Register(GlobalTag_, callback);
        }

        public void Register<T>(int tag, Action<T> callback) where T : BaseEvent
        {
            var eventName = GetEventName<T>();
            EventListenerImpl<T> listenerImpl = null;
            
            if (EventList_.TryGetValue(eventName, out var value))
            {
                listenerImpl = value as EventListenerImpl<T>;
            }
            else
            {
                listenerImpl = new EventListenerImpl<T>();
                EventList_.Add(eventName, listenerImpl);
            }

            listenerImpl?.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : BaseEvent
        {
            UnRegister(GlobalTag_, callback);
        }
        
        public void UnRegister<T>(int tag, Action<T> callback) where T : BaseEvent
        {
            var eventName = GetEventName<T>();

            if (EventList_.TryGetValue(eventName, out var value))
            {
                if (value is EventListenerImpl<T> listenerImpl)
                {
                    listenerImpl.UnRegister(tag, callback);
                }
            }
        }

        public void UnRegisterAll(int tag)
        {
            foreach (var chunk in EventList_)
            {
                chunk.Value.UnRegisterAll(tag);
            }
        }
    }
}