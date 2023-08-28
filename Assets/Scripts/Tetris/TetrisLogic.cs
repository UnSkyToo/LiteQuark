using LiteQuark.Runtime;
using UnityEngine;

namespace Tetris
{
    public class TetrisLogic : ILogic, IOnGUI
    {
        private Board Board_;
        
        private int Level_;
        private int Line_;
        private int Frame_;
        
        public bool Startup()
        {
            Board_ = new Board();

            Level_ = 0;
            Line_ = 0;
            Frame_ = 0;
            
            return true;
        }

        public void Shutdown()
        {
            Board_.Dispose();
        }

        public void Tick(float deltaTime)
        {
            InputMgr.Instance.Tick(deltaTime);

            Frame_++;
            
            if (InputMgr.Instance.IsState(InputState.Left) && Frame_ % Const.LevelTranslateFrame[Level_] == 0)
            {
                Board_.TranslateLeft();
            }

            if (InputMgr.Instance.IsState(InputState.Right) && Frame_ % Const.LevelTranslateFrame[Level_] == 0)
            {
                Board_.TranslateRight();
            }

            if (InputMgr.Instance.IsState(InputState.Rotate) && Frame_ % Const.LevelRotateFrame[Level_] == 0)
            {
                Board_.Rotate();
            }

            if (InputMgr.Instance.IsState(InputState.FastFall))
            {
                Line_ += Board_.Fall();
            }

            if (Frame_ % Const.LevelFallFrame[Level_] == 0)
            {
                Line_ += Board_.Fall();
            }

            if (Line_ > 10)
            {
                Line_ -= 10;
                Level_++;
            }
        }

        public void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * 5);
            GUILayout.Label($"Level:{Level_}, Score:{Board_?.GetScore() ?? 0}");
        }
    }
}