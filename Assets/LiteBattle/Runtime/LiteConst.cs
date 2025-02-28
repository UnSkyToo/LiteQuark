using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    public static class LiteConst
    {
        public static class ContextKey
        {
            public const string PlayerMoveState = "Player_Move_State";

            public static string FromInputKey(LiteInputKeyType type)
            {
                switch (type)
                {
                    case LiteInputKeyType.Click:
                        return "Player_Input_Key_Click";
                    case LiteInputKeyType.Press:
                        return "Player_Input_Key_Press";
                    case LiteInputKeyType.LongPress:
                        return "Player_Input_Key_LongPress";
                }

                return "Player_Input_Key_None";
            }
        }

        public static class ContextValue
        {
            public const string Empty = "Empty";
            public const string None = "None";
            public const string True = "True";
            public const string False = "False";
            
            public const string Moving = "Moving";
        }

        public static class KeyName
        {
            public const string Attack = "Attack";
            public const string Jump = "Jump";
            public const string Slot1 = "Slot1";
            public const string Slot2 = "Slot2";
            public const string Slot3 = "Slot3";

            public static List<string> GetKeyList()
            {
                return new List<string>
                {
                    Attack,
                    Jump,
                    Slot1,
                    Slot2,
                    Slot3,
                };
            }
        }
    }
}