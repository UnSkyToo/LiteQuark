using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public class MainLogic : ILogic
    {
        private int ChessWidth = 5;
        private int ChessHeight = 5;

        private Board Board_;
        
        public void Tick(float deltaTime)
        {
            // if (Input.GetMouseButtonDown(0))
            // {
            //     var pos = ChessHelper.ScreenToBoardPos(Input.mousePosition, ChessWidth, ChessHeight);
            //     ChessHelper.BoardToWorldPos(pos.x, pos.y, ChessWidth, ChessHeight);
            // }
            
            Board_.Tick(deltaTime);
        }

        public bool Startup()
        {
            Board_ = new Board(ChessWidth, ChessHeight);
            // AssetManager.Instance.LoadAsset<GameObject>("chessboard/prefab/board.prefab", (board) =>
            // {
            //     Object.Instantiate(board);
            // });

            return true;
        }

        public void Shutdown()
        {
            Board_.Dispose();
        }
    }
}