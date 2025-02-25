using System.Collections.Generic;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public enum LiteInputType
    {
        Click,
        DoubleClick,
        Press,
        Release,
        Pressing,
        Releasing,
        PressDown,
    }

    public sealed class LiteKeyboardInput
    {
        private sealed class KeyState
        {
            public float PressedTime = 100f;
            public float ReleasedTime = 100f;
            public int ClickCount = 0;
        }
        
        public bool Enable { get; set; } = true;

        private readonly Dictionary<KeyCode, KeyState> MapInfo_ = new Dictionary<KeyCode, KeyState>();

        public LiteKeyboardInput()
        {
            MapInfo_.Add(KeyCode.W, new KeyState());
            MapInfo_.Add(KeyCode.S, new KeyState());
            MapInfo_.Add(KeyCode.A, new KeyState());
            MapInfo_.Add(KeyCode.D, new KeyState());
            MapInfo_.Add(KeyCode.J, new KeyState());
            MapInfo_.Add(KeyCode.K, new KeyState());
            MapInfo_.Add(KeyCode.L, new KeyState());
            MapInfo_.Add(KeyCode.I, new KeyState());
            MapInfo_.Add(KeyCode.Space, new KeyState());
        }

        public void Update(float deltaTime)
        {
            if (!Enable)
            {
                return;
            }
            
            foreach (var map in MapInfo_)
            {
                map.Value.PressedTime += deltaTime;
                map.Value.ReleasedTime += deltaTime;

                if (Input.GetKeyDown(map.Key) && map.Value.ClickCount == 0)
                {
                    OnKeyDown(map.Value);
                }

                if (Input.GetKeyUp(map.Key) && map.Value.ClickCount != 0)
                {
                    OnKeyUp(map.Value);
                }
            }
        }

        public bool CheckKeyState(KeyCode key, LiteInputType type, float deltaTime)
        {
            if (!Enable)
            {
                return false;
            }

            if (!MapInfo_.ContainsKey(key))
            {
                MapInfo_.Add(key, new KeyState());
            }

            var clickCount = MapInfo_[key].ClickCount;
            var pressedTime = MapInfo_[key].PressedTime;
            var releasedTime = MapInfo_[key].ReleasedTime;
            var flag = false;

            switch (type)
            {
                case LiteInputType.Click:
                    flag = pressedTime == 0f;
                    break;
                case LiteInputType.DoubleClick:
                    flag = clickCount == 2 && pressedTime == 0f;
                    break;
                case LiteInputType.Press:
                    flag = clickCount != 0 && (pressedTime < 0.3f && pressedTime + deltaTime >= 0.3f);
                    break;
                case LiteInputType.Release:
                    flag = clickCount == 0 && releasedTime == 0f;
                    break;
                case LiteInputType.Pressing:
                    flag = clickCount != 0;
                    break;
                case LiteInputType.Releasing:
                    flag = clickCount == 0;
                    break;
                case LiteInputType.PressDown:
                    flag = clickCount == 1 && pressedTime == 0;
                    break;
            }

            return flag;
        }

        private void OnKeyDown(KeyState state)
        {
            state.ClickCount = state.PressedTime >= 0.4f ? 1 : 2;
            state.PressedTime = 0f;
        }

        private void OnKeyUp(KeyState state)
        {
            state.ClickCount = 0;
            state.ReleasedTime = 0f;
        }
    }
}