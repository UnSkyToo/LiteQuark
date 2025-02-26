using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteRotateController : ITick
    {
        private readonly LiteEntity Entity_;
        
        private Quaternion TargetRotation_ = Quaternion.identity;
        private float RotateSpeed_ = 0f;
        private bool UpdateRotation_ = false;
        
        public LiteRotateController(LiteEntity entity)
        {
            Entity_ = entity;
        }

        public void Tick(float deltaTime)
        {
            if (UpdateRotation_)
            {
                Entity_.Rotation = Quaternion.Slerp(Entity_.Rotation, TargetRotation_, RotateSpeed_ * deltaTime);

                if (Quaternion.Angle(Entity_.Rotation, TargetRotation_) < 1)
                {
                    Entity_.Rotation = TargetRotation_;
                    UpdateRotation_ = false;
                }
            }
        }

        public void RotateTo(Vector3 dir, float rotateSpeed)
        {
            dir = dir.normalized;
            TargetRotation_ = Quaternion.LookRotation(dir);
            RotateSpeed_ = rotateSpeed;
            UpdateRotation_ = true;
        }
    }
}