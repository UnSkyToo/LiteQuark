using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteCard.GamePlay
{
    public sealed class VariableHandler : Singleton<VariableHandler>
    {
        private delegate float Handler(AgentBase caster, AgentBase target, string[] paramList);
        private readonly Dictionary<string, Handler> HandlerList_;

        public VariableHandler()
        {
            HandlerList_ = new Dictionary<string, Handler>
            {
                { "$CASTER_STR", Handle_CasterStrength },
                { "$TARGET_STR", Handle_TargetStrength },
                { "$CASTER_DEX", Handle_CasterDexterity },
                { "$TARGET_DEX", Handle_TargetDexterity },
                { "$CASTER_ARMOUR", Handle_CasterArmour },
                { "$TARGET_ARMOUR", Handle_TargetArmour },
                { "$CASTER_ATTR", Handle_CasterAttr },
                { "$TARGET_ATTR", Handle_TargetAttr },
                { "$CASTER_BUFF", Handle_CasterBuff },
                { "$TARGET_BUFF", Handle_TargetBuff },
                { "$CARD_TAG", Handle_CardTag },
                { "$CAST_CARD", Handle_CastCard },
                { "$DEDUCT_HP", Handle_DeductHp },
                { "$DEDUCT_ARMOUR", Handle_DeductArmour },
                { "$CONTEXT", Handle_Context },
            };
        }

        public string[] GetVariableList()
        {
            return HandlerList_.Keys.ToArray();
        }

        public float? Calculate(AgentBase caster, AgentBase target, string token)
        {
            var key = token;
            var paramList = ParseArguments(token);
            if (paramList.Length != 0)
            {
                key = token.Substring(0, token.IndexOf('<'));
            }
            
            if (HandlerList_.TryGetValue(key, out var func))
            {
                return func.Invoke(caster, target, paramList);
            }
            
            Log.Info($"error variable : {token}");
            return null;
        }

        private float Handle_CasterStrength(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (caster == null)
            {
                return 0;
            }
            
            return caster.Strength;
        }
        
        private float Handle_TargetStrength(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (target == null)
            {
                return 0;
            }
            
            return target.Strength;
        }

        private float Handle_CasterDexterity(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (caster == null)
            {
                return 0;
            }

            return caster.Dexterity;
        }
        
        private float Handle_TargetDexterity(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (target == null)
            {
                return 0;
            }

            return target.Dexterity;
        }
        
        private float Handle_CasterArmour(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (caster == null)
            {
                return 0;
            }

            return caster.Armour;
        }
        
        private float Handle_TargetArmour(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (target == null)
            {
                return 0;
            }

            return target.Armour;
        }

        private float Handle_CasterAttr(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (caster == null)
            {
                return 0;
            }
            
            var attrType = ParseEnum<AgentAttrType>(paramList[0]);
            return caster.GetAttr(attrType);
        }

        private float Handle_TargetAttr(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (target == null)
            {
                return 0;
            }
            
            var attrType = ParseEnum<AgentAttrType>(paramList[0]);
            return target.GetAttr(attrType);
        }

        private float Handle_CasterBuff(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (caster == null)
            {
                return 0;
            }

            var buffID = int.Parse(paramList[0]);
            var buff = caster.GetBuffByID(buffID);

            if (buff != null)
            {
                return buff.Layer;
            }

            return 0;
        }

        private float Handle_TargetBuff(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (target == null)
            {
                return 0;
            }

            var buffID = int.Parse(paramList[0]);
            var buff = target.GetBuffByID(buffID);

            if (buff != null)
            {
                return buff.Layer;
            }

            return 0;
        }

        private float Handle_CardTag(AgentBase caster, AgentBase target, string[] paramList)
        {
            var player = AgentSystem.Instance.GetPlayer();

            var deckTypes = ParseEnumArray<CardDeckType>(paramList[0], '&');
            var tags = ParseEnumArray<CardTag>(paramList[1], '&');
            var count = 0;

            foreach (var deckType in deckTypes)
            {
                var deck = player.GetCardDeck(deckType);

                foreach (var card in deck.GetCards())
                {
                    if (card.ContainTagAny(tags))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private float Handle_CastCard(AgentBase caster, AgentBase target, string[] paramList)
        {
            var card = BattleContext.Current[BattleContextKey.CastCard] as CardBase;
            var type = paramList[0];

            switch (type)
            {
                case "Cost":
                    return card?.GetCost() ?? 0;
                case "CastCount":
                    return card?.GetData().CastCount ?? 0;
                case "UpgradeLevel":
                    return card?.GetUpgradeLevel() ?? 0;
                default:
                    Log.Info($"error CastCard sub type : {type}");
                    break;
            }

            return 0;
        }

        private float Handle_DeductHp(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (BattleContext.Current[BattleContextKey.DeductHp] is int val)
            {
                return val;
            }

            return 0;
        }
        
        private float Handle_DeductArmour(AgentBase caster, AgentBase target, string[] paramList)
        {
            if (BattleContext.Current[BattleContextKey.DeductArmour] is int val)
            {
                return val;
            }

            return 0;
        }

        private float Handle_Context(AgentBase caster, AgentBase target, string[] paramList)
        {
            var contextKey = ParseEnum<BattleContextKey>(paramList[0]);
            
            if (BattleContext.Current[contextKey] is int val)
            {
                return val;
            }

            return 0;
        }

        private string[] ParseArguments(string token)
        {
            var startIndex = token.IndexOf('<');
            if (startIndex == -1)
            {
                return Array.Empty<string>();
            }

            if (!token.EndsWith(">"))
            {
                Log.Info($"error token argument : {token}");
                return Array.Empty<string>();
            }

            var arg = token.Substring(startIndex + 1, token.Length - startIndex - 2);
            var chunks = arg.Split(',');
            var result = new List<string>();

            foreach (var chunk in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    continue;
                }
                
                result.Add(chunk);
            }

            return result.ToArray();
        }

        private T ParseEnum<T>(string token) where T : struct
        {
            return Enum.Parse<T>(token);
        }

        private T[] ParseEnumArray<T>(string token, char split) where T : struct
        {
            var result = new List<T>();
            var chunks = SplitString(token, split);

            foreach (var chunk in chunks)
            {
                var val = Enum.Parse<T>(chunk);
                result.Add(val);
            }

            return result.ToArray();
        }

        private string[] SplitString(string token, char split)
        {
            var chunks = token.Split(split);
            var result = new List<string>();

            foreach (var chunk in chunks)
            {
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    result.Add(chunk);
                }
            }

            return result.ToArray();
        }
    }
}