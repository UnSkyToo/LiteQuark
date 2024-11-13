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

        private int SelectIndex_ = InvalidIndex;
        private Vector2 ScrollPos_ = Vector2.zero;
        private List<T> Data_ = new List<T>();

        protected LiteListView()
        {
        }

        public void Reset()
        {
            SelectIndex_ = InvalidIndex;
            ScrollPos_ = Vector2.zero;
            Data_.Clear();
        }

        public void RefreshData()
        {
            OnDataChanged();
        }

        public T GetSelectItem()
        {
            if (SelectIndex_ >= 0 && SelectIndex_ < Data_.Count)
            {
                return Data_[SelectIndex_];
            }

            return default;
        }
        
        public void Draw()
        {
            LiteEditorStyle.Generate();
            
            SelectIndex_ = ClampSelectIndex(SelectIndex_);
            
            DrawDataList();
            
            DrawToolbar();
        }

        private void DrawDataList()
        {
            ScrollPos_ = EditorGUILayout.BeginScrollView(ScrollPos_);
            
            using (new GUILayout.VerticalScope())
            {
                for (var i = 0; i < Data_.Count; ++i)
                {
                    var selected = SelectIndex_ == i;
                    var oldSelected = selected;

                    using (new GUILayout.HorizontalScope())
                    {
                        selected = DrawItem(i, selected, Data_[i]);
                    }
                    GUILayout.Space(2);

                    if (selected)
                    {
                        SelectIndex_ = i;
                    }
                    else if (oldSelected)
                    {
                        SelectIndex_ = InvalidIndex;
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
                if (SelectIndex_ != -1)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginDisabledGroup(!EnableOrderControl);
                        if (GUILayout.Button("上移") && SelectIndex_ - 1 >= 0)
                        {
                            GUI.FocusControl(null);
                            SwapItem(SelectIndex_, SelectIndex_ - 1);
                            SelectIndex_--;
                        }
                        if (GUILayout.Button("下移") && SelectIndex_ + 1 < Data_.Count)
                        {
                            GUI.FocusControl(null);
                            SwapItem(SelectIndex_, SelectIndex_ + 1);
                            SelectIndex_++;
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
                            if (SelectIndex_ >= 0)
                            {
                                if (ChangeItem(SelectIndex_))
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
                            SelectIndex_ = Data_.Count - 1;
                        }
                    }
                    if (GUILayout.Button("删除"))
                    {
                        GUI.FocusControl(null);
                        if (SelectIndex_ >= 0)
                        {
                            if (DeleteItem(SelectIndex_))
                            {
                                OnDataChanged();
                                SelectIndex_ = ClampSelectIndex(SelectIndex_);
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
            return Data_ == null ? InvalidIndex : Mathf.Clamp(index, -1, Data_.Count - 1);
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
            return index >= 0 && index < Data_.Count ? Data_[index] : default;
        }

        protected void SetItem(int index, T value)
        {
            if (index >= 0 && index < Data_.Count)
            {
                Data_[index] = value;
            }
        }

        protected void OnDataChanged()
        {
            Data_ = GetList();
        }

        protected abstract List<T> GetList();
        protected abstract bool CreateItem();
        protected abstract bool DeleteItem(int index);
        protected abstract bool ChangeItem(int index);
        protected abstract void SwapItem(int index1, int index2);
    }
}