using System;
using System.Collections.Generic;
using System.Reflection;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public static class GameUtils
    {
        public static T[] AssignmentArray<T>(T[]? paramList)
        {
            if (paramList == null)
            {
                return Array.Empty<T>();
            }

            return paramList;
        }

        public static int FindArrayIndex(int[] array, int value)
        {
            for (var index = 0; index < array.Length; ++index)
            {
                if (array[index] == value)
                {
                    return index;
                }
            }

            return -1;
        }

        public static Monster RandomMonster()
        {
            var index = MathUtils.RandInt(0, AgentSystem.Instance.GetMonsterCount());
            var monster = AgentSystem.Instance.GetMonster(index);
            return monster;
        }

        public static int CalculateDamageValue(AgentBase caster, float baseValue, float strengthRate)
        {
            var strength = caster.Strength * strengthRate;
            var value = baseValue + strength;
            return (int)Math.Floor(value);
        }

        public static int CalculateArmourValue(AgentBase caster, float baseValue, float percent, float dexterityRate)
        {
            var dexterity = caster.Dexterity * dexterityRate;
            var value = baseValue * (1.0f + percent) + dexterity;
            return (int)Math.Floor(value);
        }

        public static int CalculateFloorValue(float baseValue, float percent)
        {
            var value = baseValue * percent;
            return (int)Math.Floor(value);
        }

        public static int CalculateFloorValue(float value)
        {
            return (int)Math.Floor(value);
        }

        public static int GetCardCountByType(CardDeckType deckType, CardType type)
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck(deckType);
            return deck.GetCount(type);
        }

        public static int GetCardCount(CardDeckType deckType)
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck(deckType);
            return deck.Count;
        }

        private static bool CheckParamCount(object[] paramList, int count)
        {
            if (paramList.Length != count)
            {
                Log.Info($"error param count, current is {paramList.Length}, need {count}");
                return false;
            }

            return true;
        }

        private static bool CheckParamType(object[] paramList, int index, Type type)
        {
            if (paramList[index].GetType() != type)
            {
                Log.Info($"error param type, current is {paramList[index].GetType()}, need {type}");
                return false;
            }

            return true;
        }

        public static bool CheckParam(Type[] typeList, object[] paramList)
        {
            if (!CheckParamCount(paramList, typeList.Length))
            {
                return false;
            }

            for (var index = 0; index < typeList.Length; ++index)
            {
                if (!CheckParamType(paramList, index, typeList[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckParam(object[] paramList)
        {
            if (!CheckParamCount(paramList, 0))
            {
                return false;
            }

            return true;
        }

        public static bool CheckParam<T>(object[] paramList)
        {
            if (!CheckParamCount(paramList, 1))
            {
                return false;
            }

            return CheckParamType(paramList, 0, typeof(T));
        }

        public static bool CheckParam<T1, T2>(object[] paramList)
        {
            if (!CheckParamCount(paramList, 2))
            {
                return false;
            }

            return CheckParamType(paramList, 0, typeof(T1))
                   && CheckParamType(paramList, 1, typeof(T2));
        }

        public static bool CheckParam<T1, T2, T3>(object[] paramList)
        {
            if (!CheckParamCount(paramList, 3))
            {
                return false;
            }

            return CheckParamType(paramList, 0, typeof(T1))
                   && CheckParamType(paramList, 1, typeof(T2))
                   && CheckParamType(paramList, 2, typeof(T3));
        }
        
        public static bool CheckParam<T1, T2, T3, T4>(object[] paramList)
        {
            if (!CheckParamCount(paramList, 4))
            {
                return false;
            }

            return CheckParamType(paramList, 0, typeof(T1))
                   && CheckParamType(paramList, 1, typeof(T2))
                   && CheckParamType(paramList, 2, typeof(T3))
                   && CheckParamType(paramList, 4, typeof(T4));
        }
        
        
        
        private static readonly Dictionary<string, Type> EnumTypeCache_ = new Dictionary<string, Type>();
        public static Type GetEnumType(string enumName)
        {
            if (EnumTypeCache_.ContainsKey(enumName))
            {
                return EnumTypeCache_[enumName];
            }

            foreach (var type in Assembly.GetAssembly(typeof(GameUtils)).GetTypes())
            {
                if (type.IsEnum && type.FullName.Contains(enumName))
                {
                    EnumTypeCache_.Add(enumName, type);
                    return type;
                }
            }
            
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsEnum && type.FullName.Contains(enumName))
                {
                    EnumTypeCache_.Add(enumName, type);
                    return type;
                }
            }

            return null;
        }
    }
}