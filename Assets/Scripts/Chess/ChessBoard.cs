using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public enum ChessKind : byte
    {
        None,
        White,
        Black,
    }
    
    public class ChessBoard : ITick, IDispose
    {
        public int Width { get; }
        public int Height { get; }

        private GameObject Go_;
        private ChessKind[,] Data_;
        private ChessKind CurrentKind_;
        
        public ChessBoard(int width, int height)
        {
            Width = width;
            Height = height;
            
            GenerateBoard();
            CurrentKind_ = ChessKind.White;
        }

        public void Tick(float deltaTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoChess(Input.mousePosition);
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Go_);
        }

        public void DoChess(Vector2 screenPos)
        {
            var pos = ScreenToBoardPos(Input.mousePosition);
            if (pos.x < 0 || pos.x >= Width || pos.y < 0 || pos.y >= Height)
            {
                return;
            }

            if (Data_[pos.x, pos.y] != ChessKind.None)
            {
                return;
            }
            
            GenerateChess(CurrentKind_, pos.x, pos.y);
            CurrentKind_ = CurrentKind_ == ChessKind.White ? ChessKind.Black : ChessKind.White;
        }

        private void GenerateBoard()
        {
            Go_ = new GameObject("ChessBoard");
            ChessUtils.GenerateChessBoard(Go_.transform, Width, Height);

            Data_ = new ChessKind[Width, Height];
            for (var x = 0; x < Width; ++x)
            {
                for (var y = 0; y < Height; ++y)
                {
                    Data_[x, y] = ChessKind.None;
                }
            }
        }

        private void GenerateChess(ChessKind kind, int x, int y)
        {
            Data_[x, y] = kind;
            ChessUtils.GenerateChess(Go_.transform, kind, x, y, Width, Height);
        }

        private Vector2Int ScreenToBoardPos(Vector2 screenPos)
        {
            return ChessUtils.ScreenToBoardPos(screenPos, Width, Height);
        }

        private bool CheckWin(ChessKind kind, int x, int y)
        {
            var cnt = 1;
            
            // horizontal
            var hx = x - 1;
            while (hx > 0 && Data_[hx, y] == kind)
            {
                cnt++;
                hx--;
            }

            hx = x + 1;
            while (hx < Width - 1 && Data_[hx, y] == kind)
            {
                cnt++;
                hx++;
            }

            if (cnt >= 5)
            {
                return true;
            }
            
            // vertical
            cnt = 1;
            var hy = y - 1;
            while (hy > 0 && Data_[x, hy] == kind)
            {
                cnt++;
                hy--;
            }

            hy = y + 1;
            while (hy < Height - 1 && Data_[x, hy] == kind)
            {
                cnt++;
                hy++;
            }

            if (cnt >= 5)
            {
                return true;
            }

            return false;
        }
    }
}