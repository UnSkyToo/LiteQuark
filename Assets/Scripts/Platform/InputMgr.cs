using LiteQuark.Runtime;
using UnityEngine;

namespace Platform
{
    public enum InputKeyCode
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Jump = 4,
    }
    
    public class InputMgr : Singleton<InputMgr>
    {
        private const int KeyCount = 5;
        
        private readonly bool[] KeyCurrentState_;
        private readonly bool[] KeyPreviousState_;
        
        public InputMgr()
        {
            KeyCurrentState_ = new bool[KeyCount];
            KeyPreviousState_ = new bool[KeyCount];

            for (var index = 0; index < KeyCount; ++index)
            {
                KeyPreviousState_[index] = KeyCurrentState_[index] = false;
            }
        }

        public void Update()
        {
            for (var index = 0; index < KeyCount; ++index)
            {
                KeyPreviousState_[index] = KeyCurrentState_[index];
            }

            KeyCurrentState_[(int)InputKeyCode.Left] = Input.GetKey(KeyCode.A);
            KeyCurrentState_[(int)InputKeyCode.Right] = Input.GetKey(KeyCode.D);
            KeyCurrentState_[(int)InputKeyCode.Up] = Input.GetKey(KeyCode.W);
            KeyCurrentState_[(int)InputKeyCode.Down] = Input.GetKey(KeyCode.S);
            KeyCurrentState_[(int)InputKeyCode.Jump] = Input.GetKey(KeyCode.Space);
        }

        public bool IsKeyDown(InputKeyCode code)
        {
            return !KeyPreviousState_[(int)code] && KeyCurrentState_[(int)code];
        }

        public bool IsKeyUp(InputKeyCode code)
        {
            return KeyPreviousState_[(int)code] && !KeyCurrentState_[(int)code];
        }

        public bool KeyState(InputKeyCode code)
        {
            return KeyCurrentState_[(int)code];
        }
    }
}