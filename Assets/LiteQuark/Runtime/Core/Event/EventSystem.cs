using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class EventSystem : ISystem
    {
        private readonly EventModule GlobalEvent_;
        
        public EventSystem()
        {
            GlobalEvent_ = new EventModule(LiteConst.Tag);
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            GlobalEvent_.Dispose();
        }

        public EventModule CreateIndependentModule(string name)
        {
            return new EventModule(name);
        }
        
        public void Send<T>(T msg) where T : IEventData
        {
            GlobalEvent_.Send(msg);
        }

        public void Send<T>() where T : IEventData, new()
        {
            GlobalEvent_.Send<T>();
        }

        public void Register<T>(Action<T> callback) where T : IEventData
        {
            GlobalEvent_.Register(callback);
        }

        public void Register<T>(int tag, Action<T> callback) where T : IEventData
        {
            GlobalEvent_.Register(tag, callback);
        }

        public void UnRegister<T>(Action<T> callback) where T : IEventData
        {
            GlobalEvent_.UnRegister(callback);
        }
        
        public void UnRegister<T>(int tag, Action<T> callback) where T : IEventData
        {
            GlobalEvent_.UnRegister(tag, callback);
        }

        public void UnRegisterAll(int tag)
        {
            GlobalEvent_.UnRegisterAll(tag);
        }
    }
}