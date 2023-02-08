using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class AgentSystem : Singleton<AgentSystem>, IRoundLifeCycle
    {
        private readonly Player Player_;
        private readonly List<Monster> MonsterList_;
        
        public AgentSystem()
        {
            MonsterList_ = new List<Monster>();
            Player_ = new Player();
        }

        public void Init()
        {
            var monster = new Monster("goblin", 100, 100);
            monster.SetAttr(AgentAttrType.Armour, 50);
            MonsterList_.Add(monster);

            var monster2 = new Monster("orcs", 150, 150);
            MonsterList_.Add(monster2);

            var cardList = LiteRuntime.Get<ConfigSystem>().GetDataList<CardConfig>();
            foreach (var card in cardList)
            {
                CardSystem.Instance.AddCardWithID(CardDeckType.Pool, card.ID);
            }
        }
        
        public Player GetPlayer()
        {
            return Player_;
        }

        public Monster GetMonster(int index)
        {
            if (index < 0 || index >= MonsterList_.Count)
            {
                return null;
            }
            
            return MonsterList_[index];
        }

        public Monster[] GetMonsterList()
        {
            return MonsterList_.ToArray();
        }

        public int GetMonsterCount()
        {
            return MonsterList_.Count;
        }

        public AgentBase[] GetAllAgents()
        {
            var result = new List<AgentBase>();
            result.Add(Player_);
            result.AddRange(MonsterList_);
            return result.ToArray();
        }

        public void RoundBegin()
        {
            var agents = AgentSystem.Instance.GetAllAgents();
            foreach (var agent in agents)
            {
                BuffSystem.Instance.TriggerBuff(BuffTriggerType.RoundBegin, agent);
            }
        }
        
        public void RoundEnd()
        {
            Player_.ResetRecord(RecordScopeType.Round);
            
            foreach (var deck in Player_.GetCardDecks())
            {
                foreach (var card in deck.GetCards())
                {
                    card.GetData().ResetRecord(RecordScopeType.Round);
                }
            }

            foreach (var monster in MonsterList_)
            {
                monster.ResetRecord(RecordScopeType.Round);
            }
            
            var agents = GetAllAgents();
            foreach (var agent in agents)
            {
                BuffSystem.Instance.TriggerBuff(BuffTriggerType.RoundEnd, agent);
            }
            
            Player_.RoundEnd();

            foreach (var monster in MonsterList_)
            {
                monster.RoundEnd();
            }
        }
    }
}