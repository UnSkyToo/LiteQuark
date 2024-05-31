using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public abstract class DataViewBase<T> : IEditorView where T : IJsonMainConfig
    {
        public const float DataListWidthPercent = 0.3f;
        
        public string Name { get; }
        
        protected Vector2 DataScrollPos_ = Vector2.zero;
        private Vector2 InspectorScrollPos_ = Vector2.zero;
        
        protected readonly List<T> DataList_ = new List<T>();
        protected int SelectIndex_;
        
        private readonly string JsonFile_;
        private bool IsDirty_ = false;
        
        protected DataViewBase(string name, string jsonFile)
        {
            Name = name;
            JsonFile_ = jsonFile;
            IsDirty_ = false;

            SelectIndex_ = -1;
        }
        
        public T[] GetData()
        {
            return DataList_.ToArray();
        }

        public void Draw(Rect rect)
        {
            var toolbarRect = new Rect(rect.x, rect.y, rect.width * DataListWidthPercent, rect.height);
            DrawDataToolbar(toolbarRect);
            
            var dataRect = new Rect(rect.x, rect.y + 20, rect.width * DataListWidthPercent, rect.height - 20);
            using (new GUILayout.AreaScope(dataRect, GUIContent.none, "FrameBox"))
            {
                DrawDataList(new Rect(0, 0, dataRect.width, dataRect.height));
            }
            
            var inspectorRect = new Rect(rect.x + dataRect.width, rect.y, rect.width - dataRect.width, rect.height);
            using (new GUILayout.AreaScope(inspectorRect, GUIContent.none, "FrameBox"))
            {
                DrawInspector(new Rect(0, 0, inspectorRect.width, inspectorRect.height));
            }
        }

        private void DrawDataToolbar(Rect rect)
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), $"{Name} List");

            var createRect = new Rect(rect.x + 90, rect.y, 70, 20);
            if (GUI.Button(createRect, " Create"))
            {
                CreateItem();
            }

            if (SelectIndex_ != -1)
            {
                var deleteRect = new Rect(rect.x + 165, rect.y, 70, 20);
                if (GUI.Button(deleteRect, "Delete"))
                {
                    DeleteItem(SelectIndex_);
                }

                var duplicateRect = new Rect(rect.x + 240, rect.y, 70, 20);
                if (GUI.Button(duplicateRect, "Duplicate"))
                {
                    DuplicateItem(SelectIndex_);
                }
            }

            var sortRect = new Rect(rect.xMax - 70, rect.y, 70, 20);
            if (GUI.Button(sortRect, "Sort"))
            {
                SortDataList();
            }
        }

        protected virtual void DrawDataList(Rect rect)
        {
        }

        private void DrawInspector(Rect rect)
        {
            EditorGUILayout.LabelField("Inspector");
            if (SelectIndex_ < 0 || SelectIndex_ >= DataList_.Count)
            {
                return;
            }

            InspectorScrollPos_ = EditorGUILayout.BeginScrollView(InspectorScrollPos_);
            
            EditorGUI.BeginChangeCheck();
            DataList_[SelectIndex_] = (T)ViewUtils.DrawData("Inspector", DataList_[SelectIndex_], typeof(T));
            if (EditorGUI.EndChangeCheck())
            {
                SetDirty();
            }
            
            EditorGUILayout.EndScrollView();
        }

        protected void SetDirty()
        {
            IsDirty_ = true;
            OnDataChange();
        }

        protected virtual void OnDataChange()
        {
        }
        
        private void CreateItem()
        {
            var data = TypeUtils.CreateInstance<T>();
            DataList_.Add(data);
            OnItemCreated(data);
            SelectIndex_ = DataList_.IndexOf(data);
            SetDirty();
        }

        protected virtual void OnItemCreated(T data)
        {
        }

        private void DeleteItem(int index)
        {
            var data = DataList_[index];
            DataList_.RemoveAt(index);
            OnItemDeleted(data);
            SelectIndex_ = -1;
            SetDirty();
        }

        protected virtual void OnItemDeleted(T data)
        {
        }

        private void DuplicateItem(int index)
        {
            var data = (T)DataList_[index].Clone();
            DataList_.Add(data);
            OnItemDuplicated(data);
            SelectIndex_ = DataList_.IndexOf(data);
            SetDirty();
        }

        protected virtual void OnItemDuplicated(T data)
        {
        }
        
        protected void SortDataList()
        {
            int SortFunc(T a, T b)
            {
                if (a.GetMainID() < b.GetMainID())
                {
                    return -1;
                }

                if (a.GetMainID() > b.GetMainID())
                {
                    return 1;
                }

                return 0;
            }
            
            DataList_.Sort(SortFunc);
            SetDirty();
        }

        public void Load()
        {
            // Debug.Log($"read {Name} data");
            // var text = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonFile_);
            // var data = JsonUtils.DecodeArray(text.text, typeof(T)).Cast<T>().ToArray();
            // DataList_.Clear();
            // DataList_.AddRange(data);
            // SortDataList();
        }

        public void Save()
        {
            if (IsDirty_)
            {
                // var jsonText = JsonUtils.EncodeArray(DataList_.ToArray());
                // var dir = Path.GetDirectoryName(JsonFile_);
                // if (!Directory.Exists(dir))
                // {
                //     Directory.CreateDirectory(dir);
                // }
                // File.WriteAllText(JsonFile_, jsonText);
                IsDirty_ = false;
            }
        }
    }
}