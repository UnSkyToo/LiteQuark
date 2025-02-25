using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityMovementModule : LiteEntityModuleBase
    {
        private readonly LiteMoveController MoveController_;
        private readonly LiteRotateController RotateController_;
        
        public LiteEntityMovementModule(LiteEntity entity)
            : base(entity)
        {
            MoveController_ = new LiteMoveController(entity);
            RotateController_ = new LiteRotateController(entity);
        }

        public override void Dispose()
        {
        }

        public override void Tick(float deltaTime)
        {
            MoveController_.Tick(deltaTime);
            RotateController_.Tick(deltaTime);
        }
        
        public void MoveToDir(Vector3 moveDir)
        {   
            MoveController_.MoveToDir(moveDir, 7.0f);
            RotateController_.RotateTo(moveDir, 25.0f);
            
            Entity.SetContext(LiteConst.ContextKey.PlayerMoveState, LiteConst.ContextValue.Moving);
        }

        public void MoveToPos(Vector3 pos)
        {
            MoveController_.MoveToPos(pos, 7.0f);
            var dir = (pos - Entity.Position).normalized;
            RotateController_.RotateTo(dir, 25.0f);
        }

        public void StopMove()
        {
            MoveController_.StopMove();
            
            Entity.SetContext(LiteConst.ContextKey.PlayerMoveState, LiteConst.ContextValue.None);
        }
    }
}