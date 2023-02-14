using LiteCard.GamePlay;
using UnityEditor;

namespace LiteCard.Editor
{
    // Job(xx)_Rarity(xx)_Type(xx)_Number(xxx)
    // 10_10_10_001
    [IDBindType(typeof(CardConfig))]
    public sealed class CardIDBinder : IIDBinder
    {
        public CardIDBinder()
        {
        }
        
        public int ToID(object instance, int id)
        {
            var cfg = instance as CardConfig;
            return (int)cfg.Job * 10000000 + (int)cfg.Rarity * 100000 + (int)cfg.Type * 1000 + cfg.Number;
        }

        public void Draw(string title, object instance, int id)
        {
            EditorGUILayout.IntField(title, ToID(instance, id));
        }
    }
}