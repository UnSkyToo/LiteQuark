namespace LiteCard.GamePlay
{
    public sealed class BattleLogic : IBattleLifeCycle, IRoundLifeCycle
    {
        public BattleLogic()
        {
        }

        public void BattleBegin()
        {
            RoundBegin();
        }

        public void BattleEnd()
        {
            RoundEnd();
            
            var player = AgentSystem.Instance.GetPlayer();
            player.ResetRecord(RecordScopeType.Battle);
            
            foreach (var deck in player.GetCardDecks())
            {
                foreach (var card in deck.GetCards())
                {
                    card.GetData().ResetRecord(RecordScopeType.Battle);
                }
            }

            foreach (var monster in AgentSystem.Instance.GetMonsterList())
            {
                monster.ResetRecord(RecordScopeType.Battle);
            }
        }

        public void RoundBegin()
        {
            BattleContext.Current.Reset();
            AgentSystem.Instance.RoundBegin();
            CardSystem.Instance.RoundBegin();
        }

        public void RoundEnd()
        {
            BattleContext.Current.Reset();
            AgentSystem.Instance.RoundEnd();
            CardSystem.Instance.RoundEnd();
        }
    }
}