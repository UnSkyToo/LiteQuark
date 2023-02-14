using UnityEngine;

namespace LiteCard.Editor
{
    public interface IEditorView
    {
        string Name { get; }

        void Draw(Rect rect);
        void Load();
        void Save();
    }
}