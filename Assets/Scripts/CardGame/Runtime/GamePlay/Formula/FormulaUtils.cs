namespace LiteCard.GamePlay
{
    public static class FormulaUtils
    {
        public static int Calculate(AgentBase caster, AgentBase target, string formula)
        {
            var expr = new Expression(formula);
            var value = expr.Calculate(caster, target);
            if (expr.IsError())
            {
                Log.Error($"error formula : {formula}");
                return 0;
            }
            
            return GameUtils.CalculateFloorValue(value);
        }

        public static string FormatCardDescription(Player player, CardBase card)
        {
            var data = card.GetCastData();
            var desc = data.Cfg.Desc;
            
            if (desc.Contains("<DAMAGE>"))
            {
                var modifier = GetModifierData(data.Modifiers, ModifierType.Damage);
                if (modifier != null)
                {
                    var value = Calculate(player, null, (string)modifier.ParamList[0]);
                    desc = desc.Replace("<DAMAGE>", value.ToString());
                }
            }

            if (desc.Contains("<ARMOUR>"))
            {
                var modifier = GetModifierData(data.Modifiers, ModifierType.Armour);
                if (modifier != null)
                {
                    var value = Calculate(player, null, (string)modifier.ParamList[0]);
                    desc = desc.Replace("<ARMOUR>", value.ToString());
                }
            }

            return desc;
        }

        private static ModifierData GetModifierData(ModifierData[] modifierSets, ModifierType type)
        {
            foreach (var modifier in modifierSets)
            {
                if (modifier.Cfg.Type == type)
                {
                    return modifier;
                }
            }

            return null;
        }
    }
}