using System.Collections.Generic;

namespace LiteGamePlay.Chess.AI.Valuation
{
    /// <summary>
    /// 棋组类型
    /// </summary>
    public enum TupleType
    {
        None = 0,
        Both = 1,
        Blank = 2,
        Own_One = 3,
        Own_Two = 4,
        Own_Three = 5,
        Own_Four = 6,
        Own_Five = 7,
        Other_One = 8,
        Other_Two = 9,
        Other_Three = 10,
        Other_Four = 11,
        Other_Five = 12,
    }

    /// <summary>
    /// 棋组的方向
    /// </summary>
    public enum TupleOrientation
    {
        LtoR,
        RtoL,
        TtoB,
        BtoT,
        LTtoRB,
        RBtoLT,
        LBtoRT,
        RTtoLB,
    }

    public struct TupleData
    {
        public List<ChessCoord> CoordList { get; set; }
        public TupleType Type { get; }
        public int Value { get; }

        public TupleData(TupleType type, int value)
        {
            Type = type;
            Value = value;
            CoordList = new List<ChessCoord>();
        }

        public bool ContainCoord(ChessCoord coord)
        {
            return ContainCoord(coord.X, coord.Y);
        }

        public bool ContainCoord(int x, int y)
        {
            if (CoordList == null)
            {
                return false;
            }

            foreach (var coord in CoordList)
            {
                if (coord.X == x && coord.Y == y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}