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
        private readonly ReorderableList List_;
        private readonly Type ElementType_;
        private readonly bool IsList_;
        private bool IsFoldout_;
        private float Ident_;
        private object[] Attributes_;

        private LiteReorderableListWrap(IList data)
        {
            IsList_ = TypeUtils.IsListType(data.GetType());
            ElementType_ = TypeUtils.GetElementType(data.GetType());
            
            List_ = new ReorderableList(data, ElementType_, true, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                onAddCallback = OnAddCallback,
                onRemoveCallback = OnRemoveCallback
            };

            IsFoldout_ = true;
        }

        public void Draw(GUIContent title, object[] attrs = null)
        {
            if (List_ == null)
            {
                return;
            }

            if (!DrawListHeader(title))
            {
                return;
            }

            EditorGUI.indentLevel++;
            Ident_ = EditorGUI.indentLevel * 15;
            Attributes_ = attrs;
            var rect = EditorGUILayout.GetControlRect(false, List_.GetHeight());
            rect.xMin += Ident_;
            List_.DoList(rect);
            EditorGUI.indentLevel--;
        }

        private bool DrawListHeader(GUIContent title)
        {
            var list = List_.list;
            var count = list.Count;

            using (new GUILayout.HorizontalScope())
            {
                IsFoldout_ = EditorGUILayout.Foldout(IsFoldout_, title);
                count = Mathf.Clamp(EditorGUILayout.DelayedIntField(count, GUILayout.Width(80)), 0, int.MaxValue);
            }
            EditorGUILayout.Space();

            var diff = count - list.Count;
            while (diff < 0)
            {
                RemoveAtList(List_.count - 1);
                diff++;
            }

            while (diff > 0)
            {
                AddToList(TypeUtils.CreateInstance(ElementType_));
                diff--;
            }

            return IsFoldout_;
        }

        private object DrawElementField(Rect rect, int index, object element)
        {
            return LiteEditorDrawer.DrawElement(rect, new GUIContent($"Item {index}"), element, ElementType_, Attributes_);
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
                List_.list[index] = DrawElementField(rect, index, List_.list[index]);
            //}
        }

        private void OnAddCallback(ReorderableList list)
        {
            var element = TypeUtils.CreateInstance(ElementType_);
            AddToList(element);
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            RemoveAtList(List_.index);
        }

        private void AddToList(object element)
        {
            if (IsList_)
            {
                List_.list.Add(element);
            }
            else
            {
                var newList = Array.CreateInstance(ElementType_, List_.list.Count + 1) as IList;
                for (var i = 0; i < List_.list.Count; ++i)
                {
                    newList[i] = List_.list[i];
                }

                newList[newList.Count - 1] = element;
                List_.list = newList;
            }
        }

        private void RemoveAtList(int index)
        {
            if (IsList_)
            {
                List_.list.RemoveAt(index);
            }
            else
            {
                var newList = Array.CreateInstance(ElementType_, List_.list.Count - 1) as IList;
                for (var i = 0; i < index; ++i)
                {
                    newList[i] = List_.list[i];
                }

                for (var i = index; i < List_.list.Count - 1; ++i)
                {
                    newList[i] = List_.list[i + 1];
                }

                List_.list = newList;
            }
        }

        private static readonly Dictionary<object, LiteReorderableListWrap> ReorderableWraps_ = new Dictionary<object, LiteReorderableListWrap>();
        [InitializeOnLoadMethod]
        private static void Clear()
        {
            ReorderableWraps_.Clear();
        }

        internal static LiteReorderableListWrap GetWrap(IList data)
        {
            if (data == null)
            {
                return null;
            }

            if (!ReorderableWraps_.ContainsKey(data))
            {
                ReorderableWraps_.Add(data, new LiteReorderableListWrap(data));
            }

            return ReorderableWraps_[data];
        }
    }
}