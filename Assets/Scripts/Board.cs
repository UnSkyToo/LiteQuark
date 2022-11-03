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
    
    public class Board : ISubstance
    {
        public int Width { get; }
        public int Height { get; }

        private GameObject Go_;
        private ChessKind[,] Data_;
        private ChessKind CurrentKind_;
        
        public Board(int width, int height)
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
            ChessHelper.GenerateChessBoard(Go_.transform, Width, Height);

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
            ChessHelper.GenerateChess(Go_.transform, kind, x, y, Width, Height);
        }

        private Vector2Int ScreenToBoardPos(Vector2 screenPos)
        {
            return ChessHelper.ScreenToBoardPos(screenPos, Width, Height);
        }
    }
}