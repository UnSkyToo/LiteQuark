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
        public LiteUnit Unit { get; private set; }
        
        private Vector2 CurrentMoveDir_ = Vector2.zero;

        public LitePlayerController(LiteUnit unit)
        {
            Unit = unit;
            
            LiteInputManager.Instance.EnableKeyboard = true;
            // LiteInputManager.Instance.EnableTouch = true;

            LiteInputManager.Instance.JoystickMoveStart += OnJoystickMoveStartHandler;
            LiteInputManager.Instance.JoystickMove += OnJoystickMoveHandler;
            LiteInputManager.Instance.JoystickMoveEnd += OnJoystickMoveEndHandler;
            LiteInputManager.Instance.Click += OnClickHandler;
            LiteInputManager.Instance.KeyDown += OnKeyDownHandler;
            LiteInputManager.Instance.KeyUp += OnKeyUpHandler;
            LiteInputManager.Instance.LongPressStart += OnLongPressStartHandler;
            LiteInputManager.Instance.LongPressEnd += OnLongPressEndHandler;
        }

        public void Dispose()
        {
            LiteInputManager.Instance.EnableKeyboard = false;
            // LiteInputManager.Instance.EnableTouch = false;

            LiteInputManager.Instance.JoystickMoveStart -= OnJoystickMoveStartHandler;
            LiteInputManager.Instance.JoystickMove -= OnJoystickMoveHandler;
            LiteInputManager.Instance.JoystickMoveEnd -= OnJoystickMoveEndHandler;
            LiteInputManager.Instance.Click -= OnClickHandler;
            LiteInputManager.Instance.KeyDown -= OnKeyDownHandler;
            LiteInputManager.Instance.KeyUp -= OnKeyUpHandler;
            LiteInputManager.Instance.LongPressStart -= OnLongPressStartHandler;
            LiteInputManager.Instance.LongPressEnd -= OnLongPressEndHandler;
        }

        private void OnJoystickMoveStartHandler(Vector2 dir)
        {
            dir = dir.normalized;
            if (Unit != null)
            {
                Unit.GetModule<LiteEntityMovementModule>().MoveToDir(new Vector3(dir.x, 0, dir.y));
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

            if (Unit != null)
            {
                Unit.GetModule<LiteEntityMovementModule>().MoveToDir(new Vector3(dir.x, 0, dir.y));
                CurrentMoveDir_ = dir;
            }
        }

        private void OnJoystickMoveEndHandler(Vector2 dir)
        {
            if (Unit != null)
            {
                Unit.GetModule<LiteEntityMovementModule>().StopMove();
                CurrentMoveDir_ = Vector2.zero;
            }
        }
        
        private void OnClickHandler(LitePlayerInputKey inputKey)
        {
            if (Unit != null)
            {
                Unit.SetContext(LiteInputKeyType.Click.ToContextKey(), inputKey.ToString(), 1);
            }
        }
        
        private void OnKeyDownHandler(LitePlayerInputKey inputKey)
        {
            if (Unit != null)
            {
                Unit.SetContext(LiteInputKeyType.Press.ToContextKey(), inputKey.ToString());
            }
        }

        private void OnKeyUpHandler(LitePlayerInputKey inputKey)
        {
            if (Unit != null)
            {
                Unit.SetContext(LiteInputKeyType.Press.ToContextKey(), LiteConst.ContextValue.None);
            }
        }

        private void OnLongPressStartHandler(LitePlayerInputKey inputKey)
        {
            if (Unit != null)
            {
                Unit.SetContext(LiteInputKeyType.LongPress.ToContextKey(), inputKey.ToString());
            }
        }

        private void OnLongPressEndHandler(LitePlayerInputKey inputKey)
        {
            if (Unit != null)
            {
                Unit.SetContext(LiteInputKeyType.LongPress.ToContextKey(), LiteConst.ContextValue.None);
            }
        }
    }
}