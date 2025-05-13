using System;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class SearchListWindow : EditorWindow
    {
        private static Rect _windowRect = new Rect(0, 0, 1, 1);
        public static void Draw(string title, string[] list, Action<string> callback)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(title))
            {
                GUI.FocusControl(null);
                PopupWindow.Show(_windowRect, new SearchListWindowContent(list, callback));
            }
            if (Event.current.type == EventType.Repaint) _windowRect = GUILayoutUtility.GetLastRect();
            EditorGUILayout.EndHorizontal();
        }
    }
    
    internal sealed class SearchListWindowContent : PopupWindowContent
    {
        private readonly string[] _list;
        private readonly Action<string> _callback;
        
        private string _search;
        private Vector2 _scrollPos;

        public SearchListWindowContent(string[] list, Action<string> callback)
        {
            _list = list;
            _callback = callback;
            
            _search = string.Empty;
            _scrollPos = Vector2.zero;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 400);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Space(30);
            _search = EditorGUILayout.TextField("", _search, EditorStyles.toolbarSearchField, GUILayout.MaxWidth(editorWindow.position.x / 3));
            if (GUILayout.Button("", string.IsNullOrEmpty(_search) ? "SearchCancelButtonEmpty" : "SearchCancelButton"))
            {
                _search = string.Empty;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            foreach (var val in _list)
            {
                if (val.ToLower().Contains(_search.ToLower()))
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
                _callback?.Invoke(val);
                editorWindow.Close();
            }
        }
    }
}