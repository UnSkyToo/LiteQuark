using System;
using LiteBattle.Runtime;
using LiteQuark.Runtime;

namespace LiteQuark.Demo
{
    public class DemoLogic : ILogic
    {
        public DemoLogic()
        {
        }
        
        public void Initialize(Action<bool> callback)
        {
            LiteNexusEngine.Instance.Startup();
            callback?.Invoke(true);
        }

        public void Dispose()
        {
            LiteNexusEngine.Instance.Shutdown();
        }
        
        public void Tick(float deltaTime)
        {
            LiteNexusEngine.Instance.Tick(deltaTime);
        }
    }
}