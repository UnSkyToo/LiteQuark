using System;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public class LiteInputTextDialog : EditorWindow
    {
        private Action<string> _onValueInput;
        private string _value;

        public static void ShowDialog(string title, string value, Action<string> valueInput)
        {
            var win = GetWindow<LiteInputTextDialog>(true, title);
            win._onValueInput = valueInput;
            win._value = value;
            win.minSize = new Vector2(200, 50);
            win.maxSize = new Vector2(200, 50);
            win.Show();
        }

        private void OnGUI()
        {
            _value = EditorGUILayout.TextField(_value);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Submit"))
            {
                _onValueInput?.Invoke(_value);
                Close();
            }
        }
    }
}