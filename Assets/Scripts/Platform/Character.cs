using LiteQuark.Runtime;
using UnityEngine;

namespace Platform
{
    public class Character : MovingObject
    {
        enum CharacterState
        {
            Stand,
            Walk,
            Jump,
        }

        private CharacterState State_;
        private bool OnGround_;
        private bool WasOnGround_;
        private bool OnOnewayGround_;
        private bool WasOnOnewayGround_;
        private bool AtCeiling_;
        private bool WasAtCeiling_;
        private bool PushLeftWall_;
        private bool WasPushLeftWall_;
        private bool PushRightWall_;
        private bool WasPushRightWall_;

        public Character(PhysicsWorld world, GameObject go, AABB bounds)
            : base(world, go, bounds)
        {
            State_ = CharacterState.Stand;
            OnGround_ = true;
            OnOnewayGround_ = false;
            WasOnGround_ = true;
        }

        private bool KeyState(InputKeyCode code)
        {
            return InputMgr.Instance.KeyState(code);
        }

        private Map GetMap()
        {
            return World.GetMap();
        }

        public override void Update(float time)
        {
            switch (State_)
            {
                case CharacterState.Stand:
                    OnStateStand(time);
                    break;
                case CharacterState.Walk:
                    OnStateWalk(time);
                    break;
                case CharacterState.Jump:
                    OnStateJump(time);
                    break;
            }
            
            UpdatePhysics(time);
        }

        private void UpdatePhysics(float time)
        {
            WasOnGround_ = OnGround_;

            var groundY = 0.0f;
            var ceilingY = 0.0f;
            var rightWallX = 0.0f;
            var leftWallX = 0.0f;
            var onOnewayGround = false;
            
            base.Update(time);

            if (Speed_.y <= 0f
                && CheckAtGround(OldPosition_, Position_, Speed_, out groundY, ref onOnewayGround))
            {
                Position_.y = groundY + Bounds_.HalfSize.y - Bounds_.Offset.y;
                Speed_.y = 0f;
                OnGround_ = true;
            }
            else
            {
                OnGround_ = false;
            }

            if (Speed_.x <= 0f && CheckAtLeftWall(OldPosition_, Position_, out leftWallX))
            {
                if (OldPosition_.x - Bounds_.HalfSize.x + Bounds_.Offset.x >= leftWallX)
                {
                    Position_.x = leftWallX + Bounds_.HalfSize.x - Bounds_.Offset.x;
                    PushLeftWall_ = true;
                }
            
                Speed_.x = Mathf.Max(Speed_.x, 0f);
            }
            else
            {
                PushLeftWall_ = false;
            }
            
            if (Speed_.x >= 0f && CheckAtRightWall(OldPosition_, Position_, out rightWallX))
            {
                if (OldPosition_.x + Bounds_.HalfSize.x + Bounds_.Offset.x <= rightWallX)
                {
                    Position_.x = rightWallX - Bounds_.HalfSize.x - Bounds_.Offset.x;
                    PushRightWall_ = true;
                }
            
                Speed_.x = Mathf.Min(Speed_.x, 0f);
            }
            else
            {
                PushRightWall_ = false;
            }
            
            if (Speed_.y >= 0f && CheckAtCeiling(OldPosition_, Position_, out ceilingY))
            {
                Position_.y = ceilingY - Bounds_.HalfSize.y - Bounds_.Offset.y - 1f;
                Speed_.y = 0f;
                AtCeiling_ = true;
            }
            else
            {
                AtCeiling_ = false;
            }

            OnOnewayGround_ = onOnewayGround;
        }

        private void OnStateStand(float time)
        {
            Speed_ = Vector2.zero;

            if (!OnGround_)
            {
                State_ = CharacterState.Jump;
                return;
            }
                    
            if (KeyState(InputKeyCode.Left) != KeyState(InputKeyCode.Right))
            {
                State_ = CharacterState.Walk;
            }

            if (KeyState(InputKeyCode.Jump))
            {
                Speed_.y = Const.JumpSpeed;
                State_ = CharacterState.Jump;
            }
            
            if (KeyState(InputKeyCode.Down))
            {
                if (OnOnewayGround_)
                {
                    Position_.y -= Const.OneWayPlatformThreshold;
                }
            }
        }

        private void OnStateWalk(float time)
        {
            if (KeyState(InputKeyCode.Left) == KeyState(InputKeyCode.Right))
            {
                State_ = CharacterState.Stand;
            }
                    
            if (KeyState(InputKeyCode.Left))
            {
                Speed_.x = -Const.WalkSpeed;
            }

            if (KeyState(InputKeyCode.Right))
            {
                Speed_.x = Const.WalkSpeed;
            }
            
            if (KeyState(InputKeyCode.Down))
            {
                if (OnOnewayGround_)
                {
                    Position_.y -= Const.OneWayPlatformThreshold;
                }
            }

            if (KeyState(InputKeyCode.Jump))
            {
                Speed_.y = Const.JumpSpeed;
                State_ = CharacterState.Jump;
                return;
            }
            else if (!OnGround_)
            {
                State_ = CharacterState.Jump;
                return;
            }
        }

        private void OnStateJump(float time)
        {
            Speed_.y -= time * Const.Gravity;
            Speed_.y = Mathf.Max(Speed_.y, Const.MaxFallingSpeed);
                    
            if (!KeyState(InputKeyCode.Jump) && Speed_.y > 0.0f)
            {
                Speed_.y = Mathf.Min(Speed_.y, Const.MinJumpSpeed);
            }
                    
            if (KeyState(InputKeyCode.Left) == KeyState(InputKeyCode.Right))
            {
                Speed_.x = 0;
            }
                    
            if (KeyState(InputKeyCode.Left))
            {
                Speed_.x = -Const.WalkSpeed;
            }
                    
            if (KeyState(InputKeyCode.Right))
            {
                Speed_.x = Const.WalkSpeed;
            }
                    
            if (OnGround_)
            {
                if (KeyState(InputKeyCode.Left) == KeyState(InputKeyCode.Right))
                {
                    State_ = CharacterState.Stand;
                    Speed_ = Vector2.zero;
                }
                else
                {
                    State_ = CharacterState.Walk;
                    Speed_.y = 0.0f;
                }
            }
        }

        private bool CheckAtGround(Vector2 oldPosition, Vector2 position, Vector2 speed, out float groundY, ref bool onOnewayGround)
        {
            var oldCenter = oldPosition + Bounds_.Offset;
            var newCenter = position + Bounds_.Offset;
            var oldBottomLeft = (oldCenter - Bounds_.HalfSize + Vector2.down + Vector2.right).Round();
            var newBottomLeft = (newCenter - Bounds_.HalfSize + Vector2.down + Vector2.right).Round();
            // var newBottomRight = new Vector2(newBottomLeft.x + Bounds_.HalfSize.x * 2.0f - 2.0f, newBottomLeft.y);

            var endY = GetMap().GetMapTileYAtPoint(newBottomLeft.y);
            var beginY = Mathf.Max(GetMap().GetMapTileYAtPoint(oldBottomLeft.y) - 1, endY);
            var distance = Mathf.Max(Mathf.Abs(endY - beginY), 1);

            for (var tileIndexY = beginY; tileIndexY >= endY; --tileIndexY)
            {
                var bottomLeft = Vector2.Lerp(newBottomLeft, oldBottomLeft, (float)Mathf.Abs(endY - tileIndexY) / distance);
                var bottomRight = new Vector2(bottomLeft.x + Bounds_.HalfSize.x * 2.0f - 2.0f, bottomLeft.y);

                for (var checkedTile = bottomLeft;; checkedTile.x += Const.TileSizeX)
                {
                    checkedTile.x = Mathf.Min(checkedTile.x, bottomRight.x);
                    
                    var tileIndexX = GetMap().GetMapTileXAtPoint(checkedTile.x);

                    groundY = (float)GetMap().GetMapTilePositionY(tileIndexY) + Const.TileSizeY * 0.5f;

                    if (GetMap().IsObstacle(tileIndexX, tileIndexY))
                    {
                        onOnewayGround = false;
                        return true;
                    }
                    else if (GetMap().IsOneWay(tileIndexX, tileIndexY)
                             && Mathf.Abs(checkedTile.y - groundY) <= Const.OneWayPlatformThreshold + oldPosition.y - position.y)
                    {
                        onOnewayGround = true;
                    }

                    if (checkedTile.x >= bottomRight.x)
                    {
                        if (onOnewayGround)
                        {
                            return true;
                        }
                        
                        break;
                    }
                }
            }

            groundY = 0;
            return false;
        }

        private bool CheckAtCeiling(Vector2 oldPosition, Vector2 position, out float cellingY)
        {
            var oldCenter = oldPosition + Bounds_.Offset;
            var newCenter = position + Bounds_.Offset;
            var oldTopRight = (oldCenter + Bounds_.HalfSize + Vector2.up + Vector2.left).Round();
            var newTopRight = (newCenter + Bounds_.HalfSize + Vector2.up + Vector2.left).Round();
            // var newTopLeft = new Vector2(newTopRight.x - Bounds_.HalfSize.x * 2.0f + 2.0f, newTopRight.y);

            var endY = GetMap().GetMapTileYAtPoint(newTopRight.y);
            var beginY = Mathf.Min(GetMap().GetMapTileYAtPoint(oldTopRight.y) + 1, endY);
            var distance = Mathf.Max(Mathf.Abs(endY - beginY), 1);

            for (var tileIndexY = beginY; tileIndexY <= endY; ++tileIndexY)
            {
                var topRight = Vector2.Lerp(newTopRight, oldTopRight, (float)Mathf.Abs(endY - tileIndexY) / distance);
                var topLeft = new Vector2(topRight.x - Bounds_.HalfSize.x * 2.0f + 2.0f, topRight.y);

                for (var checkedTile = topLeft;; checkedTile.x += Const.TileSizeX)
                {
                    checkedTile.x = Mathf.Min(checkedTile.x, topRight.x);
                    
                    var tileIndexX = GetMap().GetMapTileXAtPoint(checkedTile.x);

                    cellingY = (float)GetMap().GetMapTilePositionY(tileIndexY) - Const.TileSizeY * 0.5f;

                    if (GetMap().IsObstacle(tileIndexX, tileIndexY))
                    {
                        return true;
                    }

                    if (checkedTile.x >= topRight.x)
                    {
                        break;
                    }
                }
            }

            cellingY = 0;
            return false;
        }

        private bool CheckAtLeftWall(Vector2 oldPosition, Vector2 position, out float wallX)
        {
            var oldCenter = oldPosition + Bounds_.Offset;
            var newCenter = position + Bounds_.Offset;
            var oldBottomLeft = (oldCenter - Bounds_.HalfSize + Vector2.left).Round();
            var newBottomLeft = (newCenter - Bounds_.HalfSize + Vector2.left).Round();

            var endX = GetMap().GetMapTileXAtPoint(newBottomLeft.x);
            var beginX = Mathf.Min(GetMap().GetMapTileXAtPoint(oldBottomLeft.x) + 1, endX);
            var distance = Mathf.Max(Mathf.Abs(endX - beginX), 1);

            for (var tileIndexX = beginX; tileIndexX >= endX; --tileIndexX)
            {
                var bottomLeft = Vector2.Lerp(newBottomLeft, oldBottomLeft, (float)Mathf.Abs(endX - tileIndexX) / distance);
                var topLeft = new Vector2(bottomLeft.x, bottomLeft.y + Bounds_.HalfSize.y * 2.0f);

                for (var checkedTile = bottomLeft;; checkedTile.y += Const.TileSizeY)
                {
                    checkedTile.y = Mathf.Min(checkedTile.y, topLeft.y);
                    
                    var tileIndexY = GetMap().GetMapTileYAtPoint(checkedTile.y);

                    wallX = (float)GetMap().GetMapTilePositionX(tileIndexX) + Const.TileSizeX * 0.5f;

                    if (GetMap().IsObstacle(tileIndexX, tileIndexY))
                    {
                        return true;
                    }

                    if (checkedTile.y >= topLeft.y)
                    {
                        break;
                    }
                }
            }

            wallX = 0;
            return false;
        }

        private bool CheckAtRightWall(Vector2 oldPosition, Vector2 position, out float wallX)
        {
            var oldCenter = oldPosition + Bounds_.Offset;
            var newCenter = position + Bounds_.Offset;
            var oldBottomRight = (oldCenter + new Vector2(Bounds_.HalfSize.x, -Bounds_.HalfSize.y) + Vector2.right).Round();
            var newBottomRight = (newCenter + new Vector2(Bounds_.HalfSize.x, -Bounds_.HalfSize.y) + Vector2.right).Round();

            var endX = GetMap().GetMapTileXAtPoint(newBottomRight.x);
            var beginX = Mathf.Max(GetMap().GetMapTileXAtPoint(oldBottomRight.x) - 1, endX);
            var distance = Mathf.Max(Mathf.Abs(endX - beginX), 1);

            for (var tileIndexX = beginX; tileIndexX <= endX; ++tileIndexX)
            {
                var bottomRight = Vector2.Lerp(newBottomRight, oldBottomRight, (float)Mathf.Abs(endX - tileIndexX) / distance);
                var topRight = new Vector2(bottomRight.x, bottomRight.y + Bounds_.HalfSize.y * 2.0f);

                for (var checkedTile = bottomRight;; checkedTile.y += Const.TileSizeY)
                {
                    checkedTile.y = Mathf.Min(checkedTile.y, topRight.y);
                    
                    var tileIndexY = GetMap().GetMapTileYAtPoint(checkedTile.y);

                    wallX = (float)GetMap().GetMapTilePositionX(tileIndexX) - Const.TileSizeX * 0.5f;

                    if (GetMap().IsObstacle(tileIndexX, tileIndexY))
                    {
                        return true;
                    }

                    if (checkedTile.y >= topRight.y)
                    {
                        break;
                    }
                }
            }

            wallX = 0;
            return false;
        }
    }
}