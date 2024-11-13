using System;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public class LiteInputTextDialog : EditorWindow
    {
        private Action<string> OnValueInput_;
        private string Value_;

        public static void ShowDialog(string title, string value, Action<string> valueInput)
        {
            var win = GetWindow<LiteInputTextDialog>(true, title);
            win.OnValueInput_ = valueInput;
            win.Value_ = value;
            win.minSize = new Vector2(200, 50);
            win.maxSize = new Vector2(200, 50);
            win.Show();
        }

        private void OnGUI()
        {
            Value_ = EditorGUILayout.TextField(Value_);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Submit"))
            {
                OnValueInput_?.Invoke(Value_);
                Close();
            }
        }
    }
}