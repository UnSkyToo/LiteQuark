using LiteCard.GamePlay;
using UnityEditor;

namespace LiteCard.Editor
{
    // Job(xx)_Number(xxxx)
    // 10_0001
    [IDBindType(typeof(BuffConfig))]
    public sealed class BuffIDBinder : IIDBinder
    {
        public BuffIDBinder()
        {
        }
        
        public int ToID(object instance, int id)
        {
            var cfg = instance as BuffConfig;
            return (int)cfg.Job * 10000 + cfg.Number;
        }

        public void Draw(string title, object instance, int id)
        {
            EditorGUILayout.IntField(title, ToID(instance, id));
        }
    }
}