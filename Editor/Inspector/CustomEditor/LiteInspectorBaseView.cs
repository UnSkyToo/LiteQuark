using UnityEditor;

namespace LiteQuark.Editor
{
    internal abstract class LiteInspectorBaseView : UnityEditor.Editor
    {
        private bool IsCompiling_ = false;

        public sealed override void OnInspectorGUI()
        {
            if (IsCompiling_ && !EditorApplication.isCompiling)
            {
                IsCompiling_ = false;
                OnCompileCompleted();
            }
            else if (!IsCompiling_ && EditorApplication.isCompiling)
            {
                IsCompiling_ = true;
                OnCompileStart();
            }
            
            OnDraw();
            
            Repaint();
        }

        protected virtual void OnDraw()
        {
            DrawDefaultInspector();
        }

        protected virtual void OnCompileStart()
        {
        }

        protected virtual void OnCompileCompleted()
        {
        }
    }
}