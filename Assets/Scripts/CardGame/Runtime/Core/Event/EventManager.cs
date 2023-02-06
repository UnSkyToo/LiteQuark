using System;
using System.Collections.Generic;

namespace LiteCard
{
    public abstract class EventBase
    {
        public string EventName { get; }

        protected EventBase()
        {
            EventName = GetType().Name;
        }
    }
    
    public sealed class EventManager : Singleton<EventManager>
    {
        //public static event Action<EventBase> OnSend; 

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

            public override void Trigger(EventBase evt)
            {
                OnEvent?.Invoke((T)evt);
            }

#if UNITY_EDITOR
            public override void Check()
            {
                if (OnEvent != null)
                {
                    var callbackList = OnEvent.GetInvocationList();

                    foreach (var callback in callbackList)
                    {
                        Log.Warning($"{callback.Method.ReflectedType.Name} : {callback.Method.Name} UnRegister");
                    }
                }
            }
#endif
        }

        private readonly Dictionary<string, EventListener> EventList_ = new Dictionary<string, EventListener>();

        public EventManager()
        {
            EventList_.Clear();
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            foreach (var evt in EventList_)
            {
                evt.Value.Check();
            }
#endif

            //OnSend = null;
        }

        private string GetEventName<T>()
        {
            return typeof(T).Name;
        }

        public void Send<T>(T Event) where T : EventBase
        {
            if (EventList_.ContainsKey(Event.EventName))
            {
                ((EventListenerImpl<T>)EventList_[Event.EventName]).Trigger(Event);
                //OnSend?.Invoke(Event);
            }
        }

        public void Send<T>() where T : EventBase, new()
        {
            var evt = new T();
            Send(evt);
        }

        public void Register<T>(Action<T> callback) where T : EventBase
        {
            var eventName = GetEventName<T>();
            if (!EventList_.ContainsKey(eventName))
            {
                EventList_.Add(eventName, new EventListenerImpl<T>());
            }

            ((EventListenerImpl<T>)EventList_[eventName]).OnEvent += callback;
        }

        public void UnRegister<T>(Action<T> callback) where T : EventBase
        {
            var eventName = GetEventName<T>();
            if (EventList_.ContainsKey(eventName))
            {
                ((EventListenerImpl<T>)EventList_[eventName]).OnEvent -= callback;
            }
        }
    }
}