namespace LiteCard.GamePlay
{
    public interface IBattleLifeCycle
    {
        void BattleBegin();
        void BattleEnd();
    }

    public interface IRoundLifeCycle
    {
        void RoundBegin();
        void RoundEnd();
    }
}