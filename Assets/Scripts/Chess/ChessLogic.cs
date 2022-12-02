using LiteQuark.Runtime;

namespace LiteGamePlay.Chess
{
    public class ChessLogic : ILogic
    {
        public bool Startup()
        {
            ChessStageManager.Instance.ChangeTo<ChessMenuStage>();
            return true;
        }

        public void Shutdown()
        {
            ChessStageManager.Instance.ChangeToEmpty();
        }
        
        public void Tick(float deltaTime)
        {
            ChessStageManager.Instance.Tick(deltaTime);
        }
    }
}