using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteMoveController : ITick
    {
        private enum LiteMoveMode
        {
            None,
            FollowDirection,
            MoveToPosition,
        }
        
        private readonly LiteEntity Entity_;

        private LiteMoveMode MoveMode_;

        private Vector3 MoveDir_ = Vector3.zero;
        private Vector3 TargetPos_ = Vector3.zero;
        private float MoveSpeed_ = 0f;
        private float CurrentTime_ = 0;
        private float MoveTime_ = float.MaxValue;

        public LiteMoveController(LiteEntity entity)
        {
            Entity_ = entity;
        }

        public void Tick(float deltaTime)
        {
            if (MoveMode_ == LiteMoveMode.None)
            {
                return;
            }
            
            if (!Entity_.GetTag(LiteTag.CanMove))
            {
                return;
            }

            CurrentTime_ += deltaTime;
            var beginPos = Entity_.Position;
            Entity_.Position += MoveDir_ * deltaTime * MoveSpeed_;

            if (CurrentTime_ >= MoveTime_)
            {
                StopMove();
            }
        }

        public void MoveToDir(Vector3 moveDir, float moveSpeed, float moveTime = float.MaxValue)
        {
            MoveMode_ = LiteMoveMode.FollowDirection;
            
            MoveDir_ = moveDir.normalized;
            
            MoveSpeed_ = Mathf.Max(0.1f, moveSpeed);
            CurrentTime_ = 0f;
            MoveTime_ = moveTime;
        }

        public void MoveToPos(Vector3 targetPos, float moveSpeed)
        {
            MoveMode_ = LiteMoveMode.MoveToPosition;

            TargetPos_ = targetPos;
            MoveDir_ = (targetPos - Entity_.Position).normalized;
            
            MoveSpeed_ = Mathf.Max(0.1f, moveSpeed);
            CurrentTime_ = 0;
            MoveTime_ = Mathf.Max(0.01f, Vector3.Distance(Entity_.Position, TargetPos_) / MoveSpeed_);
        }

        public void StopMove()
        {
            if (MoveMode_ == LiteMoveMode.MoveToPosition)
            {
                Entity_.Position = TargetPos_;
            }
            
            MoveMode_ = LiteMoveMode.None;
        }
    }
}