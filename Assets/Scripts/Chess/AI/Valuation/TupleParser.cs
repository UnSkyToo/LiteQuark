using System.Collections.Generic;

namespace LiteGamePlay.Chess.AI.Valuation
{
    #region Tuple Parser
    /// <summary>
    /// 棋组估值器
    /// </summary>
    public class TupleParser
    {
        #region Const
        /// <summary>
        /// 棋组价值表
        /// </summary>
        private static readonly int[,] TupleValueTable_ =
        {
            {
                0,          // None
                0,          // Both
                7,          // Blank
                35,         // B
                800,        // BB
                15000,      // BBB
                800000,     // BBBB
                999999999,  // BBBBB
                15,         // W
                400,        // WW
                1800,       // WWW
                100000,     // WWWW
                999999999,  // WWWWW
            },
            {
                0,          // None
                0,          // Both
                7,          // Blank
                35,         // W
                800,        // WW
                15000,      // WWW
                800000,     // WWWW
                999999999,  // WWWWW
                15,         // B
                400,        // BB
                1800,       // BBB
                100000,     // BBBB
                999999999,  // BBBBB
            },
        };
        #endregion

        #region Variable
        private Dictionary<ChessKind, List<TupleData>> TupleMap_ = new Dictionary<ChessKind, List<TupleData>>();
        #endregion

        #region Method

        #region Public Method
        public TupleParser()
        {
            TupleMap_.Add(ChessKind.Black, new List<TupleData>());
            TupleMap_.Add(ChessKind.White, new List<TupleData>());
        }

        /// <summary>
        /// 更新指定类型的棋组价值图
        /// </summary>
        /// <param name="board">棋盘数据</param>
        /// <param name="kind">玩家当前的棋子类型</param>
        private void UpdateTupleMapWithType(ChessBoard board, ChessKind kind)
        {
            TupleMap_[kind].Clear();

            #region Horizontal

            for (var y = 0; y < board.Height; ++y)
            {
                for (var x = 0; x <= board.Width - 5; ++x)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.LtoR);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x + index, y));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            #endregion

            #region Vertical

            for (var x = 0; x < board.Width; ++x)
            {
                for (var y = 0; y <= board.Height - 5; ++y)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.TtoB);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x, y + index));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            #endregion

            #region LT-RB Corner

            for (var x = 0; x <= board.Width - 5; ++x)
            {
                for (var y = x; y <= board.Height - 5; ++y)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.LTtoRB);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x + index, y + index));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            for (var y = 0; y <= board.Height - 5; ++y)
            {
                for (var x = y + 1; x <= board.Width - 5; ++x)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.LTtoRB);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x + index, y + index));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            #endregion

            #region LB-RT Corner

            for (var x = 1; x <= board.Width - 5; ++x)
            {
                for (var y = board.Height - 1; y >= board.Height - x; --y)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.LBtoRT);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x + index, y - index));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            for (var x = 0; x <= board.Width - 5; ++x)
            {
                for (var y = board.Height - x - 1; y >= 5 - 1; --y)
                {
                    var tupleType = GetTupleTypeWithOrientation(board, kind, x, y, TupleOrientation.LBtoRT);
                    var tupleValue = GetTupleValueWithType(tupleType, kind);
                    var data = new TupleData(tupleType, tupleValue);

                    for (var index = 0; index < 5; ++index)
                    {
                        data.CoordList.Add(new ChessCoord(x + index, y - index));
                    }

                    TupleMap_[kind].Add(data);
                }
            }

            #endregion
        }

        /// <summary>
        /// 更新所有棋组价值图
        /// </summary>
        /// <param name="board">棋盘数据</param>
        public void UpdateTupleMap(ChessBoard board)
        {
            UpdateTupleMapWithType(board, ChessKind.Black);
            UpdateTupleMapWithType(board, ChessKind.White);
        }

        /// <summary>
        /// 获取指定坐标所有棋组价值
        /// </summary>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <param name="x">棋盘X坐标</param>
        /// <param name="y">棋盘Y坐标</param>
        /// <returns>棋组价值</returns>
        public int GetPointTupleValueWithMap(ChessKind kind, int x, int y)
        {
            var value = 0;

            foreach (var data in TupleMap_[kind])
            {
                if (data.ContainCoord(x, y))
                {
                    value += data.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// 获取棋盘所有坐标的棋组价值
        /// </summary>
        /// <param name="board">棋盘数据</param>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <returns>所有点的棋组价值</returns>
        private int[,] GetAllPointTupleValueWithMap(ChessBoard board, ChessKind kind)
        {
            UpdateTupleMapWithType(board, kind);
            var value = new int[board.Width, board.Height];

            for (var x = 0; x < board.Width; ++x)
            {
                for (var y = 0; y < board.Height; ++y)
                {
                    value[x, y] = GetPointTupleValueWithMap(kind, x, y);
                }
            }

            return value;
        }

        /// <summary>
        /// 获取棋盘指定类型的棋最有价值的点的价值
        /// </summary>
        /// <param name="board">棋盘数据</param>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <returns>棋组价值</returns>
        public int GetBestPointValueWithType(ChessBoard board, ChessKind kind)
        {
            var value = GetAllPointTupleValueWithMap(board, kind);
            var maxValue = 0;

            for (var x = 0; x < board.Width; ++x)
            {
                for (var y = 0; y < board.Height; ++y)
                {
                    if (maxValue < value[x, y] && board[x, y] == ChessKind.Empty)
                    {
                        maxValue = value[x, y];
                    }
                }
            }

            return maxValue;
        }

        /// <summary>
        /// 获取棋盘中指定类型的棋最有价值的所有点
        /// </summary>
        /// <param name="board">棋盘数据</param>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <returns>最优价值的点集合</returns>
        public List<ChessCoord> GetBestPointsWithType(ChessBoard board, ChessKind kind)
        {
            var value = GetAllPointTupleValueWithMap(board, kind);
            var maxValue = 0;
            var coordList = new List<ChessCoord>();

            for (var x = 0; x < board.Width; ++x)
            {
                for (var y = 0; y < board.Height; ++y)
                {
                    if (maxValue < value[x, y] && board[x, y] == ChessKind.Empty)
                    {
                        maxValue = value[x, y];
                    }
                }
            }

            for (var x = 0; x < board.Width; ++x)
            {
                for (var y = 0; y < board.Height; ++y)
                {
                    if (maxValue == value[x, y] && board[x, y] == ChessKind.Empty)
                    {
                        coordList.Add(new ChessCoord(x, y));
                    }
                }
            }

            return coordList;
        }

        #endregion

        #region Private Method
        /// <summary>
        /// 获取指定类型的棋组价值
        /// </summary>
        /// <param name="type">棋组类型</param>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <returns>棋组价值</returns>
        private int GetTupleValueWithType(TupleType type, ChessKind kind)
        {
            if (kind == ChessKind.Empty)
            {
                return 0;
            }

            switch (kind)
            {
                case ChessKind.Black:
                    return TupleValueTable_[1, (int)type];
                case ChessKind.White:
                    return TupleValueTable_[0, (int)type];
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取指定数据的棋组类型
        /// </summary>
        /// <param name="kind">当前玩家的棋子类型</param>
        /// <param name="baseType">棋组的棋子类型</param>
        /// <param name="count">棋组棋子数量</param>
        /// <returns>棋组类型</returns>
        private TupleType GetTupleTypeWithData(ChessKind kind, ChessKind baseType, int count)
        {
            if (count == 0)
            {
                return TupleType.Blank;
            }

            if (kind == baseType)
            {
                return (TupleType)((int)TupleType.Own_One + count - 1);
            }
            else
            {
                return (TupleType)((int)TupleType.Other_One + count - 1);
            }
        }

        /// <summary>
        /// 获取指定方向的棋组类型
        /// </summary>
        /// <param name="board">棋盘数据</param>
        /// <param name="expectKind">当前玩家的棋子类型</param>
        /// <param name="x">棋盘X坐标</param>
        /// <param name="y">棋盘Y坐标</param>
        /// <param name="orientation">棋组方向</param>
        /// <returns>棋组类型</returns>
        private TupleType GetTupleTypeWithOrientation(ChessBoard board, ChessKind expectKind, int x, int y, TupleOrientation orientation)
        {
            var baseType = board[x, y];

            if (baseType == ChessKind.Invalid)
            {
                return TupleType.None;
            }

            var count = 0;

            for (var index = 0; index < 5; ++index)
            {
                var kind = ChessKind.Empty;

                switch (orientation)
                {
                    case TupleOrientation.LtoR:
                        kind = board[x + index, y];
                        break;
                    case TupleOrientation.RtoL:
                        kind = board[x - index, y];
                        break;
                    case TupleOrientation.TtoB:
                        kind = board[x, y + index];
                        break;
                    case TupleOrientation.BtoT:
                        kind = board[x, y - index];
                        break;
                    case TupleOrientation.LTtoRB:
                        kind = board[x + index, y + index];
                        break;
                    case TupleOrientation.RBtoLT:
                        kind = board[x - index, y - index];
                        break;
                    case TupleOrientation.LBtoRT:
                        kind = board[x + index, y - index];
                        break;
                    case TupleOrientation.RTtoLB:
                        kind = board[x - index, y + index];
                        break;
                }

                if (kind == ChessKind.Invalid)
                {
                    return TupleType.None;
                }
                if (kind == ChessKind.Empty)
                {
                    continue;
                }
                if (kind != baseType)
                {
                    if (baseType == ChessKind.Empty)
                    {
                        baseType = kind;
                    }
                    else
                    {
                        return TupleType.Both;
                    }
                }

                count++;
            }

            return GetTupleTypeWithData(expectKind, baseType, count);
        }
        #endregion

        #endregion
    }
    #endregion
}