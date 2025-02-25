using System;
using UnityEditor;

namespace LiteBattle.Editor
{
    public interface ILiteEditorView : IDisposable
    {
        bool IsVisible();
        void Draw();
        void OnPlayModeStateChange(PlayModeStateChange state);
    }
}