using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EventSystem : ISystem
    {
        private readonly EventModule _globalEvent;
        
        public EventSystem()
        {
            _globalEvent = new EventModule(LiteConst.Tag);
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            _globalEvent.Dispose();
        }

        public EventModule CreateIndependentModule(string name)
        {
            return new EventModule(name);
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

        public void Register<T>(int tag, Action<T> callback) where T : IEventData
        {
            _globalEvent.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            _globalEvent.UnRegister(callback);
        }
        
        public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
        {
            _globalEvent.UnRegister(tag, callback);
        }

        public void UnRegisterAll(int tag)
        {
            _globalEvent.UnRegisterAll(tag);
        }
    }
}