using System;
using System.Collections;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class LiteReorderableListWrap
    {
        private readonly ReorderableList _list;
        private readonly Type _elementType;
        private readonly bool _isList;
        private bool _isFoldout;
        private float _ident;
        private object[] _attributes;

        private LiteReorderableListWrap(IList data)
        {
            _isList = TypeUtils.IsListType(data.GetType());
            _elementType = TypeUtils.GetElementType(data.GetType());
            
            _list = new ReorderableList(data, _elementType, true, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                onAddCallback = OnAddCallback,
                onRemoveCallback = OnRemoveCallback
            };

            _isFoldout = true;
        }

        public void Draw(GUIContent title, object[] attrs = null)
        {
            if (_list == null)
            {
                return;
            }

            if (!DrawListHeader(title))
            {
                return;
            }

            EditorGUI.indentLevel++;
            _ident = EditorGUI.indentLevel * 15;
            _attributes = attrs;
            var rect = EditorGUILayout.GetControlRect(false, _list.GetHeight());
            rect.xMin += _ident;
            _list.DoList(rect);
            EditorGUI.indentLevel--;
        }

        private bool DrawListHeader(GUIContent title)
        {
            var list = _list.list;
            var count = list.Count;

            using (new GUILayout.HorizontalScope())
            {
                _isFoldout = EditorGUILayout.Foldout(_isFoldout, title);
                count = Mathf.Clamp(EditorGUILayout.DelayedIntField(count, GUILayout.Width(80)), 0, int.MaxValue);
            }
            EditorGUILayout.Space();

            var diff = count - list.Count;
            while (diff < 0)
            {
                RemoveAtList(_list.count - 1);
                diff++;
            }

            while (diff > 0)
            {
                AddToList(TypeUtils.CreateInstance(_elementType));
                diff--;
            }

            return _isFoldout;
        }

        private object DrawElementField(Rect rect, int index, object element)
        {
            return LiteEditorDrawer.DrawElement(rect, new GUIContent($"Item {index}"), element, _elementType, _attributes);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            // using (new GUILayout.HorizontalScope())
            // {
                // var width = rect.width / 3 - Ident_;
                // EditorGUI.LabelField(rect, new GUIContent($"Element {index}"));
                // rect.xMin += width;
                // rect.yMin += 1f;
                // rect.yMax -= 1f;
                _list.list[index] = DrawElementField(rect, index, _list.list[index]);
            //}
        }

        private void OnAddCallback(ReorderableList list)
        {
            var element = TypeUtils.CreateInstance(_elementType);
            AddToList(element);
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            RemoveAtList(_list.index);
        }

        private void AddToList(object element)
        {
            if (_isList)
            {
                _list.list.Add(element);
            }
            else
            {
                var newList = Array.CreateInstance(_elementType, _list.list.Count + 1) as IList;
                for (var i = 0; i < _list.list.Count; ++i)
                {
                    newList[i] = _list.list[i];
                }

                newList[newList.Count - 1] = element;
                _list.list = newList;
            }
        }

        private void RemoveAtList(int index)
        {
            if (_isList)
            {
                _list.list.RemoveAt(index);
            }
            else
            {
                var newList = Array.CreateInstance(_elementType, _list.list.Count - 1) as IList;
                for (var i = 0; i < index; ++i)
                {
                    newList[i] = _list.list[i];
                }

                for (var i = index; i < _list.list.Count - 1; ++i)
                {
                    newList[i] = _list.list[i + 1];
                }

                _list.list = newList;
            }
        }

        private static readonly Dictionary<object, LiteReorderableListWrap> ReorderableWraps = new Dictionary<object, LiteReorderableListWrap>();
        [InitializeOnLoadMethod]
        private static void Clear()
        {
            ReorderableWraps.Clear();
        }

        internal static LiteReorderableListWrap GetWrap(IList data)
        {
            if (data == null)
            {
                return null;
            }

            if (!ReorderableWraps.ContainsKey(data))
            {
                ReorderableWraps.Add(data, new LiteReorderableListWrap(data));
            }

            return ReorderableWraps[data];
        }
    }
}