namespace LiteGamePlay.AI.Valuation
{
    public static class TupleUtils
    {
        private static TupleParser Parser_ = new TupleParser();

        public static ChessCoord[] GetBestValuePoints(ChessBoard ExpectBoard, ChessKind ExpectType)
        {
            var Points = Parser_.GetBestPointsWithType(ExpectBoard, ExpectType);

            return Points.ToArray();
        }
    }
}