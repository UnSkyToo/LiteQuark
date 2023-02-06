using LiteCard.GamePlay;
using UnityEditor;

namespace LiteCard.Editor
{
    // Job(xx)_Number(xxxx)
    // 10_0001
    [IDBindType(typeof(BuffConfig))]
    public sealed class BuffIDBinder : IIDBinder
    {
        private readonly BuffConfig Instance_;

        public BuffIDBinder(BuffConfig instance, int id)
        {
            Instance_ = instance;
        }
        
        public int ToID()
        {
            return (int)Instance_.Job * 10000 + Instance_.Number;
        }

        public void Draw(string title)
        {
            EditorGUILayout.IntField(title, ToID());
        }
    }
}