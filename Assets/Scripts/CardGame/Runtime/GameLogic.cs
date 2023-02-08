using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using LiteCard.UI;
using LiteQuark.Runtime;

namespace LiteCard
{
    public sealed class GameLogic : Singleton<GameLogic>
    {
        private readonly BattleLogic Battle_;

        public GameLogic()
        {
            Battle_ = new BattleLogic();
        }
        
        public void Startup()
        {
            Log.Info("Load Config...");
            
            var configs = new Dictionary<Type, string>
            {
                { typeof(MatchConfig), "CardGame/Json/match.json" },
                { typeof(ModifierConfig), "CardGame/Json/modifier.json" },
                { typeof(BuffConfig), "CardGame/Json/buff.json" },
                { typeof(CardConfig), "CardGame/Json/card.json" },
            };
            LiteRuntime.Get<ConfigSystem>().AddAssembly(typeof(GameLogic).Assembly, 0);
            LiteRuntime.Get<ConfigSystem>().LoadFromJson(configs);
            
            AgentSystem.Instance.Init();
            
            LiteRuntime.Get<UISystem>().OpenUI<UICardHand>(AgentSystem.Instance.GetPlayer().GetCardDeck(CardDeckType.Hand));
            
            LiteRuntime.Get<UISystem>().OpenUI<UIAgent>();
            LiteRuntime.Get<UISystem>().OpenUI<UIBattleMain>();
            
            Battle_.BattleBegin();
        }

        public void Shutdown()
        {
            Battle_.BattleEnd();
        }

        public void Update(float deltaTime)
        {
        }

        public BattleLogic GetBattleLogic()
        {
            return Battle_;
        }

        public void Loop()
        {
            // Battle_.BattleBegin();
            // Battle_.RoundBegin();
            //
            // while (true)
            // {
            //     Print();
            //     Input();
            //     Thread.Sleep(10);
            // }
        }

        private void Input()
        {
            // Console.WriteLine("------------------------------");
            // Console.WriteLine("draw [count] : get a card from pool");
            // Console.WriteLine("use <card> [target] : use hand card");
            // Console.WriteLine("upgrade <card> <formal> : upgrade hand card");
            // Console.WriteLine("next : next round");
            // Console.WriteLine("new : new battle");
            // Console.WriteLine("------------------------------");
            // Log.Print();
            // var chunks = ReadInput("Cmd");
            //
            // Log.Clear();
            //
            // switch (chunks[0])
            // {
            //     case "draw":
            //         if (chunks.Length == 2)
            //         {
            //             var count = int.Parse(chunks[1]);
            //             Battle_.DrawCard(count);
            //         }
            //         else if (chunks.Length == 1)
            //         {
            //             Battle_.DrawCard(1);
            //         }
            //         break;
            //     case "use":
            //         if (chunks.Length == 2)
            //         {
            //             var cardIndex = int.Parse(chunks[1]);
            //             Battle_.CastCard(CardDeckType.Hand, cardIndex);
            //         }
            //         break;
            //     case "upgrade":
            //         if (chunks.Length == 3)
            //         {
            //             var cardIndex = int.Parse(chunks[1]);
            //             var isFormal = bool.Parse(chunks[2]);
            //             Battle_.UpgradeCard(cardIndex, isFormal ? RecordScopeType.Battle : RecordScopeType.Round);
            //         }
            //         break;
            //     case "next":
            //         Battle_.RoundEnd();
            //         
            //         Battle_.RoundBegin();
            //         break;
            //     case "new":
            //         Battle_.BattleEnd();
            //         
            //         Battle_.BattleBegin();
            //         Battle_.RoundBegin();
            //         break;
            //     case "test":
            //         Test();
            //         break;
            //     case "exit":
            //         Environment.Exit(0);
            //         break;
            // }
        }

        public void SelectCard(CardDeckType deckType, Action<CardBase> callback)
        {
            throw new Exception("error");
        }
    }
}