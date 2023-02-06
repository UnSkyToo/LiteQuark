using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public sealed class HelpView : IEditorView
    {
        public string Name => "Help";
        public int Priority => int.MaxValue;

        public HelpView()
        {
        }
        
        public void Draw(Rect rect)
        {
            using (new GUILayout.AreaScope(rect, GUIContent.none, "FrameBox"))
            {
                DrawContent();
            }
        }

        private void DrawContent()
        {
            DrawList("Variable", VariableHandler.Instance.GetVariableList());
            DrawList("Context Key", Enum.GetNames(typeof(BattleContextKey)));
        }

        private readonly Dictionary<string, bool> FoldoutCache_ = new Dictionary<string, bool>();
        private void DrawList(string key, string[] list)
        {
            if (!FoldoutCache_.ContainsKey(key))
            {
                FoldoutCache_.Add(key, true);
            }

            using (new LiteEditor.ColorScope(Color.green))
            {
                FoldoutCache_[key] = EditorGUILayout.Foldout(FoldoutCache_[key], key);
            }

            if (FoldoutCache_[key])
            {
                EditorGUI.indentLevel++;
                foreach (var name in list)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(name);

                        if (GUILayout.Button("Copy"))
                        {
                            GUIUtility.systemCopyBuffer = name;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        public void Load()
        {
        }

        public void Save()
        {
        }
    }
}