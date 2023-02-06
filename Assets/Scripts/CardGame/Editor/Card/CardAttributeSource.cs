using System.Collections.Generic;
using System.Linq;
using LiteCard.GamePlay;

namespace LiteCard.Editor
{
    public static class CardAttributeSource
    {
        public static EditorDataPopupResult GetPopupData(EditorDataPopupType type)
        {
            var display = new List<string> { "None" };
            var value = new List<int> { 0 };
            
            switch (type)
            {
                case EditorDataPopupType.Card:
                    var cardView = CardEditor.GetView<ClassifyDataView<CardConfig>>(type);
                    display.AddRange(cardView.GetData().Select(data => $"{data.GetMainID()}_{data.Name}"));
                    value.AddRange(cardView.GetData().Select(data => data.GetMainID()));
                    break;
                case EditorDataPopupType.Buff:
                    var buffView = CardEditor.GetView<ClassifyDataView<BuffConfig>>(type);
                    display.AddRange(buffView.GetData().Select(data => $"{data.GetMainID()}_{data.Name}"));
                    value.AddRange(buffView.GetData().Select(data => data.GetMainID()));
                    break;
                case EditorDataPopupType.Modifier:
                    var modifierView = CardEditor.GetView<DataView<ModifierConfig>>(type);
                    display.AddRange(modifierView.GetData().Select(data => $"{data.GetMainID()}_{data.Name}"));
                    value.AddRange(modifierView.GetData().Select(data => data.GetMainID()));
                    break;
                case EditorDataPopupType.Match:
                    var matchView = CardEditor.GetView<DataView<MatchConfig>>(type);
                    display.AddRange(matchView.GetData().Select(data => $"{data.GetMainID()}_{data.Name}"));
                    value.AddRange(matchView.GetData().Select(data => data.GetMainID()));
                    break;
            }

            return new EditorDataPopupResult(display.ToArray(), value.ToArray());
        }

        public static EditorObjectArrayResult GetObjectArray(EditorObjectArrayType type, object binder)
        {
            switch (type)
            {
                case EditorObjectArrayType.CardCastCheck:
                    return CardCastCheckHandler.Instance.GetObjectArrayResult(binder);
                case EditorObjectArrayType.CardCastTarget:
                    return CardCastTargetHandler.Instance.GetObjectArrayResult(binder);
                case EditorObjectArrayType.CardUpgrade:
                    return CardUpgradeHandler.Instance.GetObjectArrayResult(binder);
                case EditorObjectArrayType.MatchFilter:
                    return MatchFilterHandler.Instance.GetObjectArrayResult(binder);
                case EditorObjectArrayType.Modifier:
                    var modifierView = CardEditor.GetView<DataView<ModifierConfig>>(EditorDataPopupType.Modifier);
                    var modifierID = (int)binder;
                    if (modifierID == 0)
                    {
                        return EditorObjectArrayResult.None;
                    }
                    
                    var cfg = modifierView.GetData().FirstOrDefault((v) => v.ID == modifierID);
                    if (cfg != null)
                    {
                        return ModifierHandler.Instance.GetObjectArrayResult(cfg.Type);
                    }
                    return null;
                case EditorObjectArrayType.BuffTrigger:
                    return BuffTriggerHandler.Instance.GetObjectArrayResult(binder);
            }
            
            return null;
        }
    }
}