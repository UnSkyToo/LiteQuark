using LiteCard.GamePlay;
using UnityEditor;

namespace LiteCard.Editor
{
    // Job(xx)_Rarity(xx)_Type(xx)_Number(xxx)
    // 10_10_10_001
    [IDBindType(typeof(CardConfig))]
    public sealed class CardIDBinder : IIDBinder
    {
        private readonly CardConfig Instance_;

        public CardIDBinder(CardConfig instance, int id)
        {
            Instance_ = instance;
        }
        
        public int ToID()
        {
            return (int)Instance_.Job * 10000000 + (int)Instance_.Rarity * 100000 + (int)Instance_.Type * 1000 + Instance_.Number;
        }

        public void Draw(string title)
        {
            EditorGUILayout.IntField(title, ToID());
        }
    }
}