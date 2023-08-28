using System.Collections.Generic;

namespace Tetris
{
    public static class Const
    {
        public const int InvalidValue = int.MaxValue;

        public const int CellWidth = 88;
        public const int CellHeight = 88;

        public const int BlockWidth = 4;
        public const int BlockHeight = 4;

        public const int BoardWidth = 10;
        public const int BoardHeight = 20;

        public const int MaxLevel = 10;
        public const float FastFallTime = 0.016f;

        public const float TranslateIntervalTime = 0.1f;
        public const float RotateIntervalTime = 0.2f;
        
        public static readonly int[] LevelFallFrame = new int[30]
        {
            48, 43, 38, 33, 28,
            23, 18, 13, 8, 6,
            5, 5, 5, 4, 4,
            4, 3, 3, 3, 2,
            2, 2, 2, 2, 2,
            2, 2, 2, 2, 1
        };

        public static readonly int[] LevelTranslateFrame = new int[30]
        {
            6, 6, 6, 6, 6,
            5, 5, 5, 5, 5,
            4, 4, 4, 4, 4,
            4, 4, 4, 4, 4,
            3, 3, 3, 3, 3,
            3, 3, 3, 3, 3
        };
        
        public static readonly int[] LevelRotateFrame = new int[30]
        {
            10, 10, 10, 10, 10,
            9, 9, 9, 9, 9,
            8, 8, 8, 8, 8,
            7, 7, 7, 7, 7,
            6, 6, 6, 6, 6,
            5, 5, 5, 5, 5
        };

        public static readonly Dictionary<BlockKind, int[,]> Blocks = new Dictionary<BlockKind, int[,]>
        {
            {
                BlockKind.I,
                new[,]
                {
                    { 0, 1, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 1, 0, 0 }
                }
            },
            {
                BlockKind.O,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 1, 1, 0 },
                    { 0, 1, 1, 0 },
                    { 0, 0, 0, 0 }
                }
            },
            {
                BlockKind.T,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 1, 1, 1, 0 },
                    { 0, 0, 0, 0 }
                }
            },
            {
                BlockKind.S,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 1, 1, 0 },
                    { 1, 1, 0, 0 },
                    { 0, 0, 0, 0 }
                }
            },
            {
                BlockKind.Z,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 1, 1, 0, 0 },
                    { 0, 1, 1, 0 },
                    { 0, 0, 0, 0 }
                }
            },
            {
                BlockKind.L,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 1, 0, 0, 0 },
                    { 1, 1, 1, 0 },
                    { 0, 0, 0, 0 }
                }
            },
            {
                BlockKind.J,
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 1, 0 },
                    { 1, 1, 1, 0 },
                    { 0, 0, 0, 0 }
                }
            }
        };
    }
}