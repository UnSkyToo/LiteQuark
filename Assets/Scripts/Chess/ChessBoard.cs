using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public class ChessBoard : ITick, IDispose
    {
        public int Width { get; }
        public int Height { get; }
        public int WinCount { get; }

        private bool IsGameOver_;
        private GameObject Go_;
        private ChessKind[,] Data_;
        private ChessKind CurrentKind_;

        private ChessManual Manual_;
        
        public ChessKind this[int x, int y] => GetChess(x, y);

        public ChessBoard(int width, int height, int winCount)
        {
            Width = width;
            Height = height;
            WinCount = winCount;
            
            GenerateBoard();
            Manual_ = new ChessManual();
            CurrentKind_ = ChessKind.Black;
            IsGameOver_ = false;
        }

        public void Tick(float deltaTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var pos = ChessUtils.ScreenToBoardPos(Input.mousePosition, Width, Height);

                if (DoChess(pos.x, pos.y))
                {
                    DoAIChess();
                }
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Go_);
        }

        private bool IsValidPos(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public ChessKind GetChess(int x, int y)
        {
            if (!IsValidPos(x, y))
            {
                return ChessKind.Invalid;
            }

            return Data_[x, y];
        }

        private void DoAIChess()
        {
            var bestCoordList = AI.Valuation.TupleUtils.GetBestValuePoints(this, CurrentKind_);
            if (bestCoordList.Length > 0)
            {
                var coord = bestCoordList[Random.Range(0, bestCoordList.Length)];
                DoChess(coord.X, coord.Y);
            }
            else
            {
                Debug.LogError("ai can not calculate point");
            }
        }

        public bool DoChess(int x, int y)
        {
            if (IsGameOver_)
            {
                return false;
            }
            
            if (GetChess(x, y) != ChessKind.Empty)
            {
                return false;
            }
            
            GenerateChess(CurrentKind_, x, y);
            CurrentKind_ = CurrentKind_ == ChessKind.White ? ChessKind.Black : ChessKind.White;
            return true;
        }

        public void DoWin(ChessKind kind, int x, int y)
        {
            if (IsGameOver_)
            {
                return;
            }
            
            Manual_.Win(kind);
            ChessDatabase.Instance.AddManual(Manual_);
            IsGameOver_ = true;
            Debug.LogError($"{kind} Win!!!");
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
                    Data_[x, y] = ChessKind.Empty;
                }
            }
        }

        private void GenerateChess(ChessKind kind, int x, int y)
        {
            Data_[x, y] = kind;
            Manual_.AddStep(x, y);
            ChessUtils.GenerateChess(Go_.transform, kind, x, y, Width, Height);

            if (CheckWin(kind, x, y))
            {
                DoWin(kind, x, y);
            }
        }

        private bool CheckWin(ChessKind kind, int x, int y)
        {
            var hCnt = GetHorizontalCount(kind, x, y);
            if (hCnt >= WinCount)
            {
                return true;
            }

            var vCnt = GetVerticalCount(kind, x, y);
            if (vCnt >= WinCount)
            {
                return true;
            }

            var cross1Cnt = GetCrossLT2RBCount(kind, x, y);
            if (cross1Cnt >= WinCount)
            {
                return true;
            }

            var cross2Cnt = GetCrossLB2RTCount(kind, x, y);
            if (cross2Cnt >= WinCount)
            {
                return true;
            }

            return false;
        }

        private int CrawlChessCount(ChessKind kind, int x, int y, int stepX, int stepY)
        {
            var cnt = 0;
            var cx = x;
            var cy = y;

            if (kind == ChessKind.Invalid)
            {
                return 0;
            }

            while (GetChess(cx, cy) == kind)
            {
                cnt++;

                cx += stepX;
                cy += stepY;
            }

            return cnt;
        }

        private int GetHorizontalCount(ChessKind kind, int x, int y)
        {
            if (GetChess(x, y) != kind)
            {
                return 0;
            }

            var leftX = CrawlChessCount(kind, x - 1, y, -1, 0);
            var rightX = CrawlChessCount(kind, x + 1, y, 1, 0);
            return leftX + rightX + 1;
        }

        private int GetVerticalCount(ChessKind kind, int x, int y)
        {
            if (GetChess(x, y) != kind)
            {
                return 0;
            }

            var topY = CrawlChessCount(kind, x, y - 1, 0, -1);
            var bottomY = CrawlChessCount(kind, x, y + 1, 0, 1);
            return topY + bottomY + 1;
        }

        private int GetCrossLT2RBCount(ChessKind kind, int x, int y)
        {
            if (GetChess(x, y) != kind)
            {
                return 0;
            }

            var lt = CrawlChessCount(kind, x - 1, y - 1, -1, -1);
            var rb = CrawlChessCount(kind, x + 1, y + 1, 1, 1);
            return lt + rb + 1;
        }

        private int GetCrossLB2RTCount(ChessKind kind, int x, int y)
        {
            if (GetChess(x, y) != kind)
            {
                return 0;
            }

            var lb = CrawlChessCount(kind, x - 1, y + 1, -1, 1);
            var rt = CrawlChessCount(kind, x + 1, y - 1, 1, -1);
            return lb + rt + 1;
        }
    }
}