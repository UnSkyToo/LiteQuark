using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public abstract class LiteListView<T>
    {
        public bool HideToolbar { get; protected set; } = false;
        public bool EnableOrderControl { get; protected set; } = true;
        
        private const int InvalidIndex = -1;

        private int _selectIndex = InvalidIndex;
        private Vector2 _scrollPos = Vector2.zero;
        private List<T> _data = new List<T>();

        protected LiteListView()
        {
        }

        public void Reset()
        {
            _selectIndex = InvalidIndex;
            _scrollPos = Vector2.zero;
            _data.Clear();
        }

        public void RefreshData()
        {
            OnDataChanged();
        }

        public T GetSelectItem()
        {
            if (_selectIndex >= 0 && _selectIndex < _data.Count)
            {
                return _data[_selectIndex];
            }

            return default;
        }
        
        public void Draw()
        {
            LiteEditorStyle.Generate();
            
            _selectIndex = ClampSelectIndex(_selectIndex);
            
            DrawDataList();
            
            DrawToolbar();
        }

        private void DrawDataList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            using (new GUILayout.VerticalScope())
            {
                for (var i = 0; i < _data.Count; ++i)
                {
                    var selected = _selectIndex == i;
                    var oldSelected = selected;

                    using (new GUILayout.HorizontalScope())
                    {
                        selected = DrawItem(i, selected, _data[i]);
                    }
                    GUILayout.Space(2);

                    if (selected)
                    {
                        _selectIndex = i;
                    }
                    else if (oldSelected)
                    {
                        _selectIndex = InvalidIndex;
                    }
                }
                GUILayout.FlexibleSpace();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            if (HideToolbar)
            {
                return;
            }
        
            using (new GUILayout.VerticalScope(LiteEditorStyle.InFooter))
            {
                if (_selectIndex != -1)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginDisabledGroup(!EnableOrderControl);
                        if (GUILayout.Button("上移") && _selectIndex - 1 >= 0)
                        {
                            GUI.FocusControl(null);
                            SwapItem(_selectIndex, _selectIndex - 1);
                            _selectIndex--;
                        }
                        if (GUILayout.Button("下移") && _selectIndex + 1 < _data.Count)
                        {
                            GUI.FocusControl(null);
                            SwapItem(_selectIndex, _selectIndex + 1);
                            _selectIndex++;
                        }
                        EditorGUI.EndDisabledGroup();

                        // if (GUILayout.Button("取消"))
                        // {
                        //     GUI.FocusControl(null);
                        //     SelectIndex_ = InvalidIndex;
                        // }

                        if (GUILayout.Button("修改"))
                        {
                            GUI.FocusControl(null);
                            if (_selectIndex >= 0)
                            {
                                if (ChangeItem(_selectIndex))
                                {
                                    OnDataChanged();
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("提示", "请选择一项", "确定");
                            }
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("添加"))
                    {
                        GUI.FocusControl(null);
                        if (CreateItem())
                        {
                            OnDataChanged();
                            _selectIndex = _data.Count - 1;
                        }
                    }
                    if (GUILayout.Button("删除"))
                    {
                        GUI.FocusControl(null);
                        if (_selectIndex >= 0)
                        {
                            if (DeleteItem(_selectIndex))
                            {
                                OnDataChanged();
                                _selectIndex = ClampSelectIndex(_selectIndex);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("提示", "请选择一项", "确定");
                        }
                    }
                }
            }
        }

        private int ClampSelectIndex(int index)
        {
            return _data == null ? InvalidIndex : Mathf.Clamp(index, -1, _data.Count - 1);
        }
        
        protected virtual bool DrawItem(int index, bool selected, T obj)
        {
            if (GUILayout.Button($"{obj}", selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
            {
                GUI.FocusControl(null);
                selected = !selected;
            }

            return selected;
        }

        protected T GetItem(int index)
        {
            return index >= 0 && index < _data.Count ? _data[index] : default;
        }

        protected void SetItem(int index, T value)
        {
            if (index >= 0 && index < _data.Count)
            {
                _data[index] = value;
            }
        }

        protected void OnDataChanged()
        {
            _data = GetList();
        }

        protected abstract List<T> GetList();
        protected abstract bool CreateItem();
        protected abstract bool DeleteItem(int index);
        protected abstract bool ChangeItem(int index);
        protected abstract void SwapItem(int index1, int index2);
    }
}