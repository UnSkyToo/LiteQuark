using System;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class SearchListWindow : EditorWindow
    {
        private static Rect WindowRect_ = new Rect(0, 0, 1, 1);
        public static void Draw(string title, string[] list, Action<string> callback)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(title))
            {
                GUI.FocusControl(null);
                PopupWindow.Show(WindowRect_, new SearchListWindowContent(list, callback));
            }
            if (Event.current.type == EventType.Repaint) WindowRect_ = GUILayoutUtility.GetLastRect();
            EditorGUILayout.EndHorizontal();
        }
    }
    
    internal sealed class SearchListWindowContent : PopupWindowContent
    {
        private readonly string[] List_;
        private readonly Action<string> Callback_;
        
        private string Search_;
        private Vector2 ScrollPos_;

        public SearchListWindowContent(string[] list, Action<string> callback)
        {
            List_ = list;
            Callback_ = callback;
            
            Search_ = string.Empty;
            ScrollPos_ = Vector2.zero;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 400);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Space(30);
            Search_ = EditorGUILayout.TextField("", Search_, EditorStyles.toolbarSearchField, GUILayout.MaxWidth(editorWindow.position.x / 3));
            if (GUILayout.Button("", string.IsNullOrEmpty(Search_) ? "SearchCancelButtonEmpty" : "SearchCancelButton"))
            {
                Search_ = string.Empty;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            ScrollPos_ = GUILayout.BeginScrollView(ScrollPos_);
            foreach (var val in List_)
            {
                if (val.ToLower().Contains(Search_.ToLower()))
                {
                    DrawItem(val);
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawItem(string val)
        {
            if (EditorGUILayout.LinkButton(val))
            {
                Callback_?.Invoke(val);
                editorWindow.Close();
            }
        }
    }
}