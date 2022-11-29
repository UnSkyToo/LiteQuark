using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal abstract class BuilderStepView
    {
        public abstract bool Enabled { get; protected set; }

        private readonly string Title_;
        private readonly Rect Rect_;

        public BuilderStepView(string title, Rect rect)
        {
            Title_ = title;
            Rect_ = rect;
        }

        public void Draw()
        {
            using (new GUILayout.AreaScope(Rect_, string.Empty, "GroupBox"))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawTitle();
                    }

                    var preColor = Handles.color;
                    Handles.color = Color.gray;
                    Handles.DrawLine(new Vector3(2, 34, 0), new Vector3(Rect_.width - 4, 34, 0));
                    Handles.color = preColor;
                    
                    GUILayout.Space(8);
                    
                    using (new EditorGUI.DisabledGroupScope(!Enabled))
                    {
                        DrawContent();
                    }
                }
            }
        }

        protected virtual void DrawTitle()
        {
            Enabled = EditorGUILayout.ToggleLeft(Title_, Enabled);
        }

        protected abstract void DrawContent();
    }
}