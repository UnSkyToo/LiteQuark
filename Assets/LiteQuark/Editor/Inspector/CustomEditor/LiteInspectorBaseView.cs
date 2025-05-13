using UnityEditor;

namespace LiteQuark.Editor
{
    internal abstract class LiteInspectorBaseView : UnityEditor.Editor
    {
        private bool _isCompiling = false;

        public sealed override void OnInspectorGUI()
        {
            if (_isCompiling && !EditorApplication.isCompiling)
            {
                _isCompiling = false;
                OnCompileCompleted();
            }
            else if (!_isCompiling && EditorApplication.isCompiling)
            {
                _isCompiling = true;
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