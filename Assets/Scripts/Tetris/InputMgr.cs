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
    
    public class InputMgr : Singleton<InputMgr>, ITick
    { 
        private InputState State_;
        private bool IsTouch_;
        private int TouchBeginFrame_;
        private Vector2 TouchBeginPos_;
        
        public InputMgr()
        {
            State_ = InputState.None;
        }

        public void Tick(float deltaTime)
        {
            State_ = InputState.None;
            UpdateKeyboard();
            UpdateMouse();
            UpdateTouch();
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
// #if !UNITY_EDITOR
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
// #endif
        }

        private void UpdateFinger()
        {
            if (Input.touchCount != 1)
            {
                return;
            }

            var touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TouchBeginPos_ = touch.position;
                    TouchBeginFrame_ = Time.frameCount;
                    IsTouch_ = true;
                    break;
                case TouchPhase.Moved:
                    
                    break;
                case TouchPhase.Ended:
                    if (IsTouch_)
                    {
                        IsTouch_ = false;
                        HandleSwipe(TouchBeginPos_, touch.position, TouchBeginFrame_, Time.frameCount);
                    }
                    break;
                case TouchPhase.Canceled:
                    break;
            }
        }

        private void HandleSwipe(Vector2 beginPos, Vector2 endPos, int beginFrame, int endFrame)
        {
            
        }
    }
}