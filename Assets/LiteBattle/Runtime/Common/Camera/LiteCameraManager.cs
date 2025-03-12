using System.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteCameraManager : Singleton<LiteCameraManager>, IManager
    {
        public Camera MainCamera => Camera_;
        
        private Camera Camera_;
        private LiteUnit Unit_;
        
        private LiteCameraManager()
        {
        }

        public Task<bool> Startup()
        {
            return Task.FromResult(true);
        }

        public void Shutdown()
        {
        }

        public void Tick(float deltaTime)
        {
            if (Unit_ == null)
            {
                return;
            }

            Camera_.transform.position = Unit_.Position + new Vector3(0, 8, -12);
            Camera_.transform.rotation = Quaternion.Euler(35, 0, 0);
        }

        public void Bind(Camera camera, LiteUnit unit)
        {
            Camera_ = camera;
            Unit_ = unit;
        }
    }
}