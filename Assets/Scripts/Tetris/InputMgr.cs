using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace Tetris
{
    public enum InputState
    {
        None,
        Left,
        Right,
        FastFall,
        Rotate,
    }

    public enum GestureDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        LeftUp,
        LeftDown,
        RightUp,
        RightDown,
    }
    
    public class InputMgr : Singleton<InputMgr>, ITick
    {
        public event Action<GestureDirection> Swipe; 
        
        private const float MinDistance = 80;
        
        private InputState State_;
        private Vector2 BeginPos_;
        private Vector2 EndPos_;
        
        public InputMgr()
        {
            State_ = InputState.None;
        }

        public void Tick(float deltaTime)
        {
            State_ = InputState.None;
            UpdateKeyboard();
            // UpdateMouse();
            UpdateTouch();
            UpdateGesture();
        }

        public bool IsState(InputState state)
        {
            return State_ == state;
        }
        
        private void UpdateKeyboard()
        {
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.W))
            {
                State_ = InputState.Rotate;
            }

            if (Input.GetKey(KeyCode.A))
            {
                State_ = InputState.Left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                State_ = InputState.Right;
            }

            if (Input.GetKey(KeyCode.S))
            {
                State_ = InputState.FastFall;
            }
#endif
        }

        private void UpdateMouse()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                var pos = Input.mousePosition;

                var w = Screen.width;
                var h = Screen.height;

                var x = pos.x;
                var y = pos.y;

                if (y < h / 8f * 1f)
                {
                    State_ = InputState.FastFall;
                }
                else if (y < h / 8f * 2f)
                {
                    if (x < w * 0.5f)
                    {
                        State_ = InputState.Left;
                    }

                    if (x > w * 0.5f)
                    {
                        State_ = InputState.Right;
                    }
                }
                else
                {
                    State_ = InputState.Rotate;
                }
            }
#endif
        }

        private void UpdateTouch()
        {
#if !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                var w = Screen.width;
                var h = Screen.height;

                var x = touch.position.x;
                var y = touch.position.y;

                if (y < h / 8f * 1f)
                {
                    State_ = InputState.FastFall;
                }
                // else if (y < h / 8f * 2f)
                // {
                //     if (x < w * 0.5f)
                //     {
                //         State_ = InputState.Left;
                //     }
                //
                //     if (x > w * 0.5f)
                //     {
                //         State_ = InputState.Right;
                //     }
                // }
                // else
                // {
                //     State_ = InputState.Rotate;
                // }
            }
#endif
        }

        private void UpdateGesture()
        {
            if (Input.touchCount != 1)
            {
                return;
            }

            var touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginPos_ = touch.position;
                    break;
                case TouchPhase.Ended:
                    EndPos_ = touch.position;
                    DispatchGesture();
                    break;
            }
        }
        
        private void DispatchGesture()
        {
            var distance = Vector2.Distance(BeginPos_, EndPos_);
            if (distance < MinDistance)
            {
                return;
            }

            var angle = MathUtils.AngleByPoint(BeginPos_, EndPos_);
            var dir = GestureDirection.None;
            // 0 - 22.5
            // 22.5 - 67.5
            // 67.5 - 112.5
            // 112.5 - 157.5
            // 157.5 - 202.5
            // 202.5 - 247.5
            // 247.5 - 292.5
            // 292.5 - 337.5
            // 337.5 - 360
            if (angle is >= 22.5f and < 67.5f)
            {
                dir = GestureDirection.RightUp;
            }
            else if (angle is >= 67.5f and < 112.5f)
            {
                dir = GestureDirection.Right;
                State_ = InputState.Right;
            }
            else if (angle is >= 112.5f and < 157.5f)
            {
                dir = GestureDirection.RightDown;
            }
            else if (angle is >= 157.5f and < 202.5f)
            {
                dir = GestureDirection.Down;
                State_ = InputState.FastFall;
            }
            else if (angle is >= 202.5f and < 247.5f)
            {
                dir = GestureDirection.LeftDown;
            }
            else if (angle is >= 247.5f and < 292.5f)
            {
                dir = GestureDirection.Left;
                State_ = InputState.Left;
            }
            else if (angle is >= 292.5f and < 337.5f)
            {
                dir = GestureDirection.LeftUp;
            }
            else
            {
                dir = GestureDirection.Up;
                State_ = InputState.Rotate;
            }

            Swipe?.Invoke(dir);
        }
    }
}