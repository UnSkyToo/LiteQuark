﻿using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class UnityInternalViewer : EditorWindow
    {
        private Vector2 _scrollPos = Vector2.zero;
        private string _searchText = string.Empty;
        private int _index = 0;
        private readonly string[] _tabList = new string[] { "Style", "Icon" };

        [MenuItem("Lite/Common/Internal Viewer")]
        private static void ShowWin()
        {
            var win = GetWindow<UnityInternalViewer>();
            win.titleContent = new GUIContent("Internal Viewer");
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Space(30);
            _searchText = EditorGUILayout.TextField(string.Empty, _searchText, "SearchTextField", GUILayout.MaxWidth(position.x / 3));
            GUILayout.Label(string.Empty, "SearchCancelButtonEmpty");
            GUILayout.EndHorizontal();

            _index = GUILayout.Toolbar(_index, _tabList);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            switch (_index)
            {
                case 0:
                    DrawStyleList();
                    break;
                case 1:
                    DrawIconList();
                    break;
            }
            GUILayout.EndScrollView();
        }

        private void CopyToClipboard(string text)
        {
            var textEditor = new TextEditor();
            textEditor.text = text;
            textEditor.OnFocus();
            textEditor.Copy();
            
            LEditorLog.Info($"copy [{text}] success!");
        }

        private void DrawStyleList()
        {
            foreach (var style in GUI.skin.customStyles)
            {
                if (style.name.ToLower().Contains(_searchText.ToLower()))
                {
                    DrawStyleItem(style);
                }
            }
        }
        
        private void DrawStyleItem(GUIStyle style)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(40);
            EditorGUILayout.SelectableLabel(style.name);
            GUILayout.FlexibleSpace();
            EditorGUILayout.SelectableLabel(style.name, style);
            GUILayout.Space(40);
            EditorGUILayout.SelectableLabel("", style, GUILayout.Height(40), GUILayout.Width(40));
            GUILayout.Space(50);
            if (GUILayout.Button("复制到剪贴板"))
            {
                CopyToClipboard(style.name);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawIconList()
        {
            var iconListInfo = typeof(EditorGUIUtility).GetField("s_IconGUIContents", BindingFlags.Static | BindingFlags.NonPublic);
            if (iconListInfo != null)
            {
                var iconList = (Hashtable)iconListInfo.GetValue(null);
                foreach (var icon in iconList)
                {
                    var item = (DictionaryEntry)icon;
                    if (item.Key.ToString().ToLower().Contains(_searchText.ToLower()))
                    {
                        DrawIconItem(item);
                    }
                }
            }
        }

        private void DrawIconItem(DictionaryEntry item)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(40);
            EditorGUILayout.SelectableLabel(item.Key.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.Box(item.Value as GUIContent);
            GUILayout.Space(50);
            if (GUILayout.Button("复制到剪贴板"))
            {
                CopyToClipboard(item.Key.ToString());
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }
}