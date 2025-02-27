using UnityEditor;

namespace LiteBattle.Editor
{
    public abstract class LiteBaseView : ILiteEditorView
    {
        public LiteNexusEditor NexusEditor { get; }
        
        protected LiteBaseView(LiteNexusEditor nexusEditor)
        {
            NexusEditor = nexusEditor;
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