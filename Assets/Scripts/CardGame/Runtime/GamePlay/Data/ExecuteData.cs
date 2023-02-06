using System;

namespace LiteCard.GamePlay
{
    public abstract class ExecuteData
    {
        public ModifierData[] Modifiers { get; private set; }

        protected void InitExecuteData(ModifierSet[] modifierSets)
        {
            if (modifierSets == null || modifierSets.Length == 0)
            {
                Modifiers = Array.Empty<ModifierData>();
                return;
            }
            
            Modifiers = new ModifierData[modifierSets.Length];
            for (var index = 0; index < modifierSets.Length; ++index)
            {
                Modifiers[index] = new ModifierData(modifierSets[index].ModifierID, modifierSets[index].ParamList);
            }
        }
    }
}