using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteCameraMgr : Singleton<LiteCameraMgr>
    {
        private Camera Camera_;
        private LiteAgent Agent_;
        
        private LiteCameraMgr()
        {
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
        }

        public void Tick(float deltaTime)
        {
            if (Agent_ == null)
            {
                return;
            }

            Camera_.transform.position = Agent_.Position + new Vector3(0, 8, -12);
            Camera_.transform.rotation = Quaternion.Euler(35, 0, 0);
        }

        public void Bind(Camera camera, LiteAgent agent)
        {
            Camera_ = camera;
            Agent_ = agent;
        }
    }
}