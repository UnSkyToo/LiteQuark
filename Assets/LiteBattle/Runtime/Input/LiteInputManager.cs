using System.Collections.Generic;
using System.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteInputManager : Singleton<LiteInputManager>, IManager
    {
        public bool EnableKeyboard
        {
            get => Keyboard_?.Enable ?? false;
            set
            {
                if (Keyboard_ != null)
                {
                    Keyboard_.Enable = value;
                }
            }
        }

        public bool EnableTouch
        {
            get => Touch_?.Enable ?? false;
            set
            {
                if (Touch_ != null)
                {
                    Touch_.Enable = value;
                }
            }
        }
        
        public event System.Action<Vector2> JoystickMoveStart;
        public event System.Action<Vector2> JoystickMove;
        public event System.Action<Vector2> JoystickMoveEnd;
        public event System.Action<LitePlayerInputKey> Click;
        public event System.Action<LitePlayerInputKey> KeyDown;
        public event System.Action<LitePlayerInputKey> KeyUp; 
        public event System.Action<LitePlayerInputKey> LongPressStart;
        public event System.Action<LitePlayerInputKey> LongPressEnd;
        public event System.Action<Vector2> TouchMove;
        public event System.Action<float> TouchScale;
        
        private LiteKeyboardInput Keyboard_;
        private LiteTouchInput Touch_;

        private Vector2 InputDir_ = Vector2.zero;
        private Vector3 SimulateInput_ = Vector3.zero;
        private Vector3 SimulateLastInput_ = Vector3.zero;
        private Vector2 SimulateMove_ = Vector2.zero;
        private Vector2 SimulateGesture_ = Vector2.zero;

        private readonly Dictionary<KeyCode, LitePlayerInputKey> InputKeyMapping_ = new Dictionary<KeyCode, LitePlayerInputKey>();

        private LiteInputManager()
        {
        }

        public Task<bool> Startup()
        {
            Keyboard_ = new LiteKeyboardInput();
            Touch_ = new LiteTouchInput(OnTouchMove, OnTouchScale);
            
            InputKeyMapping_.Add(KeyCode.J, LitePlayerInputKey.Attack);
            InputKeyMapping_.Add(KeyCode.K, LitePlayerInputKey.Slot1);
            InputKeyMapping_.Add(KeyCode.L, LitePlayerInputKey.Slot2);
            InputKeyMapping_.Add(KeyCode.I, LitePlayerInputKey.Slot3);
            InputKeyMapping_.Add(KeyCode.Space, LitePlayerInputKey.Jump);
            
            EnableKeyboard = false;
            EnableTouch = false;
            
            return Task.FromResult(true);
        }

        public void Shutdown()
        {
            JoystickMoveStart = null;
            JoystickMove = null;
            JoystickMoveEnd = null;
            Click = null;
            LongPressStart = null;
            LongPressEnd = null;
            TouchMove = null;
            TouchScale = null;

            EnableKeyboard = false;
            EnableTouch = false;
        }

        public void Tick(float deltaTime)
        {
            Touch_?.Update(deltaTime);

#if (((UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8 || UNITY_BLACKBERRY) && !UNITY_EDITOR))
#else
            UpdateKeyboard(deltaTime);
#endif
        }

        private void UpdateKeyboard(float deltaTime)
        {
            if (!EnableKeyboard)
            {
                OnJoystickMove(Vector2.zero);
                return;
            }
            
            Keyboard_.Update(deltaTime);

            var doubleClick = false;
            if (Keyboard_.CheckKeyState(KeyCode.W, LiteInputType.DoubleClick, deltaTime))
            {
                doubleClick = true;
                SimulateInput_ += new Vector3(0, 0, 1f);
            }

            if (Keyboard_.CheckKeyState(KeyCode.S, LiteInputType.DoubleClick, deltaTime))
            {
                doubleClick = true;
                SimulateInput_ += new Vector3(0, 0, -1f);
            }

            if (Keyboard_.CheckKeyState(KeyCode.A, LiteInputType.DoubleClick, deltaTime))
            {
                doubleClick = true;
                SimulateInput_ += new Vector3(-1f, 0, 0);
            }

            if (Keyboard_.CheckKeyState(KeyCode.D, LiteInputType.DoubleClick, deltaTime))
            {
                doubleClick = true;
                SimulateInput_ += new Vector3(1f, 0, 0);
            }

            if (doubleClick)
            {
                if (SimulateInput_ != Vector3.zero)
                {
                    SimulateGesture_.x = SimulateInput_.x;
                    SimulateGesture_.y = SimulateInput_.z;
                    OnSwipeEnd(SimulateGesture_);
                }
            }
            else
            {
                SimulateLastInput_ = SimulateInput_;
                SimulateInput_ = Vector3.zero;
                
                if (Input.GetKey(KeyCode.W))
                {
                    SimulateInput_ += new Vector3(0, 0, 1f);
                }

                if (Input.GetKey(KeyCode.S))
                {
                    SimulateInput_ += new Vector3(0, 0, -1f);
                }

                if (Input.GetKey(KeyCode.A))
                {
                    SimulateInput_ += new Vector3(-1f, 0, 0);
                }

                if (Input.GetKey(KeyCode.D))
                {
                    SimulateInput_ += new Vector3(1f, 0, 0);
                }

                if (SimulateInput_ != Vector3.zero)
                {
                    SimulateMove_.x = SimulateInput_.x;
                    SimulateMove_.y = SimulateInput_.z;

                    if (InputDir_ == Vector2.zero)
                    {
                        OnJoystickMoveStart(SimulateMove_);
                    }
                    
                    OnJoystickMove(SimulateMove_);
                }
                else
                {
                    if (SimulateLastInput_ != Vector3.zero)
                    {
                        OnJoystickMoveEnd(SimulateMove_);
                    }
                }
            }


            foreach (var input in InputKeyMapping_)
            {
                if (Keyboard_.CheckKeyState(input.Key, LiteInputType.Click, deltaTime))
                {
                    OnClick(input.Value);
                }

                if (Keyboard_.CheckKeyState(input.Key, LiteInputType.PressDown, deltaTime))
                {
                    OnKeyDown(input.Value);
                }

                if (Keyboard_.CheckKeyState(input.Key, LiteInputType.Release, deltaTime))
                {
                    OnKeyUp(input.Value);
                }

                if (Keyboard_.CheckKeyState(input.Key, LiteInputType.Press, deltaTime))
                {
                    OnLongPressStart(input.Value);
                }

                if (Keyboard_.CheckKeyState(input.Key, LiteInputType.Release, deltaTime))
                {
                    OnLongPressEnd(input.Value);
                }
            }
        }
        
        private void OnJoystickMoveStart(Vector2 dir)
        {
            JoystickMoveStart?.Invoke(dir);
        }

        private void OnJoystickMove(Vector2 dir)
        {
            InputDir_ = dir;
            JoystickMove?.Invoke(dir);
        }

        private void OnJoystickMoveEnd(Vector2 dir)
        {
            InputDir_ = Vector2.zero;
            JoystickMoveEnd?.Invoke(dir);
        }
        
        private void OnSwipeStart(Vector2 dir)
        {
        }

        private void OnSwipe(Vector2 dir)
        {
        }
        
        private void OnSwipeEnd(Vector2 dir)
        {
        }
        
        private void OnClick(LitePlayerInputKey inputKey)
        {
            Click?.Invoke(inputKey);
        }
        
        private void OnKeyDown(LitePlayerInputKey inputKey)
        {
            KeyDown?.Invoke(inputKey);
        }

        private void OnKeyUp(LitePlayerInputKey inputKey)
        {
            KeyUp?.Invoke(inputKey);
        }

        private void OnLongPressStart(LitePlayerInputKey inputKey)
        {
            LongPressStart?.Invoke(inputKey);
        }

        private void OnLongPressEnd(LitePlayerInputKey inputKey)
        {
            LongPressEnd?.Invoke(inputKey);
        }
        
        private void OnTouchMove(Vector2 offset)
        {
            TouchMove?.Invoke(offset);
        }

        private void OnTouchScale(float scale)
        {
            TouchScale?.Invoke(scale);
        }
    }
}