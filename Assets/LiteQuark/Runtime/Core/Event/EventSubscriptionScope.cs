using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    /// <summary>Owns a group of subscriptions in one event module.</summary>
    public sealed class EventSubscriptionScope : IDispose
    {
        private EventModule _module;
        private HashSet<Type> _eventTypes;

        internal EventSubscriptionScope(EventModule module)
        {
            _module = module;
        }

        public void Register<T>(Action<T> callback) where T : IEventData
        {
            if (_module == null)
            {
                return;
            }

            (_eventTypes ??= new HashSet<Type>()).Add(typeof(T));
            _module.Register(this, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            _module?.UnRegister(this, callback);
        }

        public void Dispose()
        {
            var module = _module;
            _module = null;
            if (_eventTypes == null)
            {
                return;
            }

            foreach (var eventType in _eventTypes)
            {
                module.RemoveScope(eventType, this);
            }
            _eventTypes = null;
        }
    }
}
