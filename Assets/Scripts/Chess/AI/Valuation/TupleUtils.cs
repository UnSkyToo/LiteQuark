namespace LiteGamePlay.Chess.AI.Valuation
{
    public static class TupleUtils
    {
        private static TupleParser Parser_ = new TupleParser();

        public static ChessCoord[] GetBestValuePoints(ChessBoard expectBoard, ChessKind expectType)
        {
            var coordList = Parser_.GetBestPointsWithType(expectBoard, expectType);

            return coordList.ToArray();
        }
    }
}