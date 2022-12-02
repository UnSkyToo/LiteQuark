namespace LiteGamePlay.Chess
{
    public sealed class ChessGameStage : ChessStageBase
    {
        public override string ViewName => "GamePage";

        private int ChessWidth = 15;
        private int ChessHeight = 15;
        private int WinCount = 5;
        
        private ChessBoard Board_;

        public override void OnOpen(params object[] args)
        {
            Board_ = new ChessBoard(ChessWidth, ChessHeight, WinCount);
        }

        public override void OnClose()
        {
            Board_.Dispose();
        }

        public override void OnTick(float deltaTime)
        {
            Board_.Tick(deltaTime);

            if (Board_.IsGameOver)
            {
                ChessStageManager.Instance.ChangeTo<ChessResultStage>(Board_.WinKind);
            }
        }
    }
}