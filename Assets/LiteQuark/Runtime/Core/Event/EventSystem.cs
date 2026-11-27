using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class EventSystem : ISystem
    {
        private readonly EventModule _globalEvent;

        public EventSystem()
        {
            _globalEvent = new EventModule(LiteConst.Tag);
        }

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            _globalEvent.Dispose();
            return UniTask.CompletedTask;
        }

        public EventModule CreateIndependentModule(string name)
        {
            return new EventModule(name);
        }

        public EventSubscriptionScope CreateSubscriptionScope()
        {
            return _globalEvent.CreateSubscriptionScope();
        }

        public void Send<T>(T msg) where T : IEventData
        {
            _globalEvent.Send(msg);
        }

        public void Send<T>() where T : IEventData, new()
        {
            _globalEvent.Send<T>();
        }

        public void Register<T>(Action<T> callback) where T : IEventData
        {
            _globalEvent.Register(callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            _globalEvent.UnRegister(callback);
        }

        internal System.Collections.Generic.Dictionary<Type, int> GetEventDebugInfo()
        {
            return _globalEvent.GetEventDebugInfo();
        }
    }
}
