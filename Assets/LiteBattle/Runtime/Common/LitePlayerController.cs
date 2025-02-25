using System;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public enum LiteInputKeyType : byte
    {
        Click,
        Press,
        LongPress,
    }
    
    public class LitePlayerController : IDisposable
    {
        public LiteAgent Agent => Agent_;
        
        private readonly LiteAgent Agent_;
        private Vector2 CurrentMoveDir_ = Vector2.zero;

        public LitePlayerController(LiteAgent agent)
        {
            Agent_ = agent;
            
            LiteInputMgr.Instance.EnableKeyboard = true;
            // LiteInputMgr.Instance.EnableTouch = true;

            LiteInputMgr.Instance.JoystickMoveStart += OnJoystickMoveStartHandler;
            LiteInputMgr.Instance.JoystickMove += OnJoystickMoveHandler;
            LiteInputMgr.Instance.JoystickMoveEnd += OnJoystickMoveEndHandler;
            LiteInputMgr.Instance.Click += OnClickHandler;
            LiteInputMgr.Instance.KeyDown += OnKeyDownHandler;
            LiteInputMgr.Instance.KeyUp += OnKeyUpHandler;
            LiteInputMgr.Instance.LongPressStart += OnLongPressStartHandler;
            LiteInputMgr.Instance.LongPressEnd += OnLongPressEndHandler;
        }

        public void Dispose()
        {
            LiteInputMgr.Instance.EnableKeyboard = false;
            // LiteInputMgr.Instance.EnableTouch = false;

            LiteInputMgr.Instance.JoystickMoveStart -= OnJoystickMoveStartHandler;
            LiteInputMgr.Instance.JoystickMove -= OnJoystickMoveHandler;
            LiteInputMgr.Instance.JoystickMoveEnd -= OnJoystickMoveEndHandler;
            LiteInputMgr.Instance.Click -= OnClickHandler;
            LiteInputMgr.Instance.KeyDown -= OnKeyDownHandler;
            LiteInputMgr.Instance.KeyUp -= OnKeyUpHandler;
            LiteInputMgr.Instance.LongPressStart -= OnLongPressStartHandler;
            LiteInputMgr.Instance.LongPressEnd -= OnLongPressEndHandler;
        }

        private void OnJoystickMoveStartHandler(Vector2 dir)
        {
            dir = dir.normalized;
            if (Agent_ != null)
            {
                Agent_.GetModule<LiteEntityMovementModule>().MoveToDir(new Vector3(dir.x, 0, dir.y));
                CurrentMoveDir_ = dir;
            }
        }

        private void OnJoystickMoveHandler(Vector2 dir)
        {
            dir = dir.normalized;
            if (CurrentMoveDir_ == dir)
            {
                return;
            }

            if (Agent_ != null)
            {
                Agent_.GetModule<LiteEntityMovementModule>().MoveToDir(new Vector3(dir.x, 0, dir.y));
                CurrentMoveDir_ = dir;
            }
        }

        private void OnJoystickMoveEndHandler(Vector2 dir)
        {
            if (Agent_ != null)
            {
                Agent_.GetModule<LiteEntityMovementModule>().StopMove();
                CurrentMoveDir_ = Vector2.zero;
            }
        }
        
        private void OnClickHandler(LitePlayerInputKey inputKey)
        {
            if (Agent_ != null)
            {
                Agent_.SetContext(LiteInputKeyType.Click.ToContextKey(), inputKey.ToString(), 1);
            }
        }
        
        private void OnKeyDownHandler(LitePlayerInputKey inputKey)
        {
            if (Agent_ != null)
            {
                Agent_.SetContext(LiteInputKeyType.Press.ToContextKey(), inputKey.ToString());
            }
        }

        private void OnKeyUpHandler(LitePlayerInputKey inputKey)
        {
            if (Agent_ != null)
            {
                Agent_.SetContext(LiteInputKeyType.Press.ToContextKey(), LiteConst.ContextValue.None);
            }
        }

        private void OnLongPressStartHandler(LitePlayerInputKey inputKey)
        {
            if (Agent_ != null)
            {
                Agent_.SetContext(LiteInputKeyType.LongPress.ToContextKey(), inputKey.ToString());
            }
        }

        private void OnLongPressEndHandler(LitePlayerInputKey inputKey)
        {
            if (Agent_ != null)
            {
                Agent_.SetContext(LiteInputKeyType.LongPress.ToContextKey(), LiteConst.ContextValue.None);
            }
        }
    }
}