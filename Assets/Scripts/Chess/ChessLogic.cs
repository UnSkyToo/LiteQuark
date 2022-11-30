using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public class ChessLogic : ILogic
    {
        private int ChessWidth = 15;
        private int ChessHeight = 15;

        private ChessBoard Board_;
        
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
            Board_ = new ChessBoard(ChessWidth, ChessHeight);
            // AssetManager.Instance.LoadAsset<GameObject>("chess/prefab/board.prefab", (board) =>
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