using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace Tetris
{
    public class Board : IDisposable
    {
        private GameObject Go_;
        private GameObject[,] Cells_;
        private int[,] Value_;
        private int Score_;
        
        private Block CurrentBlock_;
        
        public Board()
        {
            Go_ = LiteRuntime.Get<AssetSystem>().InstantiateSync("Tetris/Board.prefab");
            Go_.transform.SetParent(GameObject.Find("Canvas_Normal").transform, false);

            Cells_ = new GameObject[Const.BoardHeight, Const.BoardWidth];
            
            for (var y = 0; y < Const.BoardHeight; ++y)
            {
                for (var x = 0; x < Const.BoardWidth; ++x)
                {
                    Cells_[y, x] = LiteRuntime.Get<AssetSystem>().InstantiateSync("Tetris/Cell.prefab");
                    Cells_[y, x].name = $"Cell{y}{x}";
                    Cells_[y, x].transform.SetParent(Go_.transform.Find("Cells"), false);
                    Cells_[y, x].GetComponent<RectTransform>().anchoredPosition = new Vector2(x * Const.CellWidth, -y * Const.CellHeight);
                }
            }

            Value_ = new int[Const.BoardHeight, Const.BoardWidth];
            Score_ = 0;
            
            BindCells();
            
            NewBlock();
        }

        public void Dispose()
        {
            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    if (Cells_[y, x] != null)
                    {
                        LiteRuntime.Get<AssetSystem>().UnloadAsset(Cells_[y, x]);
                        Cells_[y, x] = null;
                    }
                }
            }

            if (Go_ != null)
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(Go_);
                Go_ = null;
            }
        }

        public int GetScore()
        {
            return Score_;
        }

        public int GetValue(int x, int y)
        {
            if (x is < 0 or >= Const.BoardWidth)
            {
                return Const.InvalidValue;
            }

            if (y is < 0 or >= Const.BoardHeight)
            {
                return Const.InvalidValue;
            }

            return Value_[y, x];
        }
        
        public void SetValue(int x, int y, int value)
        {
            if (x is < 0 or >= Const.BoardWidth)
            {
                return;
            }

            if (y is < 0 or >= Const.BoardHeight)
            {
                return;
            }

            Value_[y, x] = value;
        }

        public bool IsBlocked(int x, int y)
        {
            return GetValue(x, y) != 0;
        }

        public bool HasBlock()
        {
            return CurrentBlock_ != null;
        }

        public void NewBlock()
        {
            var index = UnityEngine.Random.Range(0, Enum.GetNames(typeof(BlockKind)).Length);
            CurrentBlock_ = new Block((BlockKind)index);
            CurrentBlock_.MoveTo(3, 0);
            AddBlock(CurrentBlock_);
        }

        public void DeleteBlock()
        {
            if (HasBlock())
            {
                CurrentBlock_.Dispose();
                CurrentBlock_ = null;
            }
        }

        public int Fall()
        {
            if (!HasBlock())
            {
                return 0;
            }

            if (OverlapCheck(CurrentBlock_.X, CurrentBlock_.Y + 1))
            {
                FillBlock();
                return HandleFullLine();
            }
            
            CurrentBlock_.Fall();
            return 0;
        }
        
        public void TranslateLeft()
        {
            if (!HasBlock())
            {
                return;
            }

            if (OverlapCheck(CurrentBlock_.X - 1, CurrentBlock_.Y))
            {
                return;
            }
            
            CurrentBlock_.TranslateLeft();
        }

        public void TranslateRight()
        {
            if (!HasBlock())
            {
                return;
            }

            if (OverlapCheck(CurrentBlock_.X + 1, CurrentBlock_.Y))
            {
                return;
            }
            
            CurrentBlock_.TranslateRight();
        }

        public void Rotate()
        {
            if (!HasBlock())
            {
                return;
            }
            
            CurrentBlock_.RotateClockwise();
            if (OverlapCheck(CurrentBlock_.X, CurrentBlock_.Y))
            {
                CurrentBlock_.RotateCounterclockwise();
                return;
            }
        }

        public void AddBlock(Block block)
        {
            block.GetTransform().SetParent(Go_.transform, false);
        }

        private void BindCells()
        {
            for (var y = 0; y < Const.BoardHeight; ++y)
            {
                for (var x = 0; x < Const.BoardWidth; ++x)
                {
                    Cells_[y, x].SetActive(IsBlocked(x, y));
                }
            }
        }

        private void FillBlock()
        {
            if (!HasBlock())
            {
                return;
            }

            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    if (CurrentBlock_.IsBlocked(x, y))
                    {
                        SetValue(CurrentBlock_.X + x, CurrentBlock_.Y + y, CurrentBlock_.GetValue(x, y));
                    }
                }
            }
            
            BindCells();
            
            DeleteBlock();
            
            NewBlock();
        }

        private bool OverlapCheck(int checkX, int checkY)
        {
            if (!HasBlock())
            {
                return false;
            }

            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    var bx = x + checkX;
                    var by = y + checkY;

                    if (IsBlocked(bx, by) && CurrentBlock_.IsBlocked(x, y))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private List<int> FullLineCheck()
        {
            var lines = new List<int>();
            for (var y = 0; y < Const.BoardHeight; ++y)
            {
                var isFull = true;
                
                for (var x = 0; x < Const.BoardWidth; ++x)
                {
                    if (!IsBlocked(x, y))
                    {
                        isFull = false;
                        break;
                    }
                }
                
                if (isFull)
                {
                    lines.Add(y);
                }
            }

            return lines;
        }

        private void RemoveLine(int line)
        {
            for (var y = line; y > 0; --y)
            {
                for (var x = 0; x < Const.BoardWidth; ++x)
                {
                    Value_[y, x] = Value_[y - 1, x];
                }
            }

            for (var x = 0; x < Const.BoardWidth; ++x)
            {
                Value_[0, x] = 0;
            }
        }

        private int HandleFullLine()
        {
            var lines = FullLineCheck();
            if (lines.Count == 0)
            {
                return 0;
            }

            foreach (var line in lines)
            {
                RemoveLine(line);
            }

            switch (lines.Count)
            {
                case 1:
                    Score_ += 1;
                    break;
                case 2:
                    Score_ += 3;
                    break;
                case 3:
                    Score_ += 6;
                    break;
                case 4:
                    Score_ += 10;
                    break;
            }
            
            BindCells();
            LiteRuntime.Get<LogSystem>().Warn($"Score = {Score_}");
            return lines.Count;
        }
    }
}