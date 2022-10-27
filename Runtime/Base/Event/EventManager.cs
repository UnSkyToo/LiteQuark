using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class EventManager : Singleton<EventManager>, IManager
    {
        private abstract class EventListener
        {
            public abstract void Trigger(EventBase msg);

#if UNITY_EDITOR
            public abstract void Check();
#endif
        }

        private class EventListenerImpl<T> : EventListener where T : EventBase
        {
            public event Action<T> OnEvent = null;

            public override void Trigger(EventBase msg)
            {
                OnEvent?.Invoke((T) msg);
            }

#if UNITY_EDITOR
            public override void Check()
            {
                if (OnEvent != null)
                {
                    var CallbackList = OnEvent.GetInvocationList();

                    foreach (var Callback in CallbackList)
                    {
                        LiteLog.Instance.Warning("LiteEngine", $"{Callback.Method.ReflectedType.Name} : {Callback.Method.Name} UnRegister");
                    }
                }
            }
#endif
        }

        private readonly Dictionary<string, EventListener> EventList_ = new Dictionary<string, EventListener>();

        public bool Startup()
        {
            EventList_.Clear();
            return true;
        }

        public void Shutdown()
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

        public void Send<T>(T msg) where T : EventBase
        {
            if (EventList_.ContainsKey(msg.EventName))
            {
                ((EventListenerImpl<T>) EventList_[msg.EventName]).Trigger(msg);
                //OnSend?.Invoke(Event);
            }
        }

        public void Send<T>() where T : EventBase, new()
        {
            var msg = new T();
            Send(msg);
        }

        public void Register<T>(Action<T> callback) where T : EventBase
        {
            var eventName = GetEventName<T>();
            if (!EventList_.ContainsKey(eventName))
            {
                EventList_.Add(eventName, new EventListenerImpl<T>());
            }

            ((EventListenerImpl<T>) EventList_[eventName]).OnEvent += callback;
        }

        public void UnRegister<T>(Action<T> callback) where T : EventBase
        {
            var eventName = GetEventName<T>();
            if (EventList_.ContainsKey(eventName))
            {
                ((EventListenerImpl<T>) EventList_[eventName]).OnEvent -= callback;
            }
        }
    }
}