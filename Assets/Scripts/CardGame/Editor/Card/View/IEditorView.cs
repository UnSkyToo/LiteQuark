using UnityEngine;

namespace LiteCard.Editor
{
    public interface IEditorView
    {
        string Name { get; }
        int Priority { get; }

        void Draw(Rect rect);
        void Load();
        void Save();
    }
}