using UnityEditor;

namespace LiteBattle.Editor
{
    public abstract class LiteViewBase : ILiteEditorView
    {
        public LiteStateEditor StateEditor { get; }
        
        protected LiteViewBase(LiteStateEditor stateEditor)
        {
            StateEditor = stateEditor;
        }

        public abstract void Dispose();
        public abstract bool IsVisible();
        public abstract void Draw();

        protected void DrawTitle(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.helpBox);
        }

        public virtual void OnPlayModeStateChange(PlayModeStateChange state)
        {
        }
    }
}