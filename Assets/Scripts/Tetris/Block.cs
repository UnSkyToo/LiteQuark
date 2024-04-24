using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace Tetris
{
    public enum BlockKind
    {
        I = 0,
        O,
        T,
        S,
        Z,
        L,
        J,
    }
    
    public class Block : IDisposable
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        
        private GameObject Go_;
        private GameObject[,] Cells_;
        private int[,] Value_;

        public Block(BlockKind kind)
        {
            Go_ = LiteRuntime.Get<AssetSystem>().InstantiateSync("Tetris/Block.prefab", null);
            
            Cells_ = new GameObject[Const.BlockHeight, Const.BlockWidth];
            
            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    Cells_[y, x] = LiteRuntime.Get<AssetSystem>().InstantiateSync("Tetris/Cell.prefab", Go_.transform);
                    Cells_[y, x].name = $"Cell{y}{x}";
                    Cells_[y, x].GetComponent<RectTransform>().anchoredPosition = new Vector2(x * Const.CellWidth, -y * Const.CellHeight);
                }
            }

            X = 0;
            Y = 0;

            Value_ = Const.Blocks[kind];
            
            BindCells();
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

        public int GetValue(int x, int y)
        {
            if (x is < 0 or >= Const.BlockWidth)
            {
                return Const.InvalidValue;
            }

            if (y is < 0 or >= Const.BlockHeight)
            {
                return Const.InvalidValue;
            }

            return Value_[y, x];
        }

        public bool IsBlocked(int x, int y)
        {
            return GetValue(x, y) != 0;
        }

        public GameObject GetGo()
        {
            return Go_;
        }

        public Transform GetTransform()
        {
            return Go_.transform;
        }

        private void UpdatePosition()
        {
            Go_.GetComponent<RectTransform>().anchoredPosition = new Vector2(X * Const.CellWidth, -Y * Const.CellHeight);
        }

        private void BindCells()
        {
            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    Cells_[y, x].SetActive(IsBlocked(x, y));
                }
            }
        }

        public void Fall()
        {
            Y++;
            UpdatePosition();
        }
        
        public void TranslateLeft()
        {
            X--;
            UpdatePosition();
        }

        public void TranslateRight()
        {
            X++;
            UpdatePosition();
        }

        public void MoveTo(int x, int y)
        {
            X = x;
            Y = y;
            UpdatePosition();
        }
        
        public void RotateClockwise()
        {
            // A1 A2 A3 A4  ->  D1 C1 B1 A1
            // B1 B2 B3 B4  ->  D2 C2 B2 A2
            // C1 C2 C3 C4  ->  D3 C3 B3 A3
            // D1 D2 D3 D4  ->  D4 C4 B4 A4

            var result = new int[Const.BlockHeight, Const.BlockWidth];

            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    result[y, x] = Value_[Const.BlockWidth - x - 1, y];
                }
            }

            Value_ = result;

            BindCells();
        }

        public void RotateCounterclockwise()
        {
            // A1 A2 A3 A4  ->  A4 B4 C4 D4
            // B1 B2 B3 B4  ->  A3 B3 C3 D3
            // C1 C2 C3 C4  ->  A2 B2 C2 D2
            // D1 D2 D3 D4  ->  A1 B1 C1 D1

            var result = new int[Const.BlockHeight, Const.BlockWidth];

            for (var y = 0; y < Const.BlockHeight; ++y)
            {
                for (var x = 0; x < Const.BlockWidth; ++x)
                {
                    result[y, x] = Value_[x, Const.BlockHeight - y - 1];
                }
            }

            Value_ = result;

            BindCells();
        }

        // public void Display()
        // {
        //     var msg = new System.Text.StringBuilder();
        //
        //     for (var y = 0; y < Const.BlockHeight; ++y)
        //     {
        //         for (var x = 0; x < Const.BlockWidth; ++x)
        //         {
        //             msg.Append(Value_[y, x]);
        //             msg.Append(" ");
        //         }
        //
        //         msg.AppendLine();
        //     }
        //
        //     Debug.Log(msg.ToString());
        // }
    }
}