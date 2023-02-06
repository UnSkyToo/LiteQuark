using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteCard.GamePlay;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public class DataView<T> : IEditorView where T : IJsonMainConfig
    {
        public const float DataListWidthPercent = 0.3f;
        public const float ItemWidth = 100f;
        public const float ItemHeight = 40f;
        
        public string Name { get; }
        public int Priority { get; }

        protected readonly List<T> DataList_ = new List<T>();
        protected Vector2 DataScrollPos_ = Vector2.zero;
        private Vector2 InspectorScrollPos_ = Vector2.zero;
        private int Index_ = -1;
        private bool IsDirty_ = false;

        private readonly string JsonFile_;

        public DataView(string name, int priority, string jsonFile)
        {
            Name = name;
            Priority = priority;
            JsonFile_ = jsonFile;
        }

        public void Draw(Rect rect)
        {
            var dataRect = new Rect(rect.x, rect.y, rect.width * DataListWidthPercent, rect.height);
            using (new GUILayout.AreaScope(dataRect, GUIContent.none, "FrameBox"))
            {
                DrawDataContent(new Rect(0, 0, dataRect.width, dataRect.height));
            }
            
            var inspectorRect = new Rect(rect.x + dataRect.width, rect.y, rect.width - dataRect.width, rect.height);
            using (new GUILayout.AreaScope(inspectorRect, GUIContent.none, "FrameBox"))
            {
                DrawInspector(new Rect(0, 0, inspectorRect.width, inspectorRect.height));
            }
        }

        private void DrawDataContent(Rect rect)
        {
            DrawDataToolbar(rect);
            DrawDataList(rect);
        }

        private void DrawDataToolbar(Rect rect)
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), $"{Name} List");

            var createRect = new Rect(rect.x + 50, rect.y, 80, 20);
            if (GUI.Button(createRect, " Create"))
            {
                CreateItem();
            }

            if (Index_ != -1)
            {
                var deleteRect = new Rect(rect.x + 140, rect.y, 80, 20);
                if (GUI.Button(deleteRect, "Delete"))
                {
                    DeleteItem(Index_);
                }

                var duplicateRect = new Rect(rect.x + 230, rect.y, 80, 20);
                if (GUI.Button(duplicateRect, "Duplicate"))
                {
                    DuplicateItem(Index_);
                }
            }

            var sortRect = new Rect(rect.xMax - 80, rect.y, 80, 20);
            if (GUI.Button(sortRect, "Sort"))
            {
                SortDataList();
            }
        }

        protected virtual void DrawDataList(Rect rect)
        {
            var xCount = Mathf.FloorToInt(rect.width / ItemWidth);
            var yCount = Mathf.CeilToInt((float)DataList_.Count / (float)xCount);
            var height = Mathf.Max(rect.height, yCount * ItemHeight);
            
            var dataRect = new Rect(rect.x, rect.y + 20, rect.width, rect.height - 20);
            DataScrollPos_ = GUI.BeginScrollView(dataRect, DataScrollPos_, new Rect(dataRect.x, dataRect.y, dataRect.width, height));

            for (var index = 0; index < DataList_.Count; ++index)
            {
                var y = index / xCount;
                var x = index % xCount;
                var itemRect = new Rect(dataRect.x + x * ItemWidth, dataRect.y + y * ItemHeight, ItemWidth, ItemHeight);
                DrawDataItem(itemRect, index);
            }
            
            GUI.EndScrollView();
        }

        protected void DrawDataItem(Rect rect, int index)
        {
            var data = DataList_[index];

            if (Index_ == index)
            {
                using (new LiteEditor.ColorScope(Color.red))
                {
                    GUI.Box(rect, Texture2D.redTexture);
                }
            }
            
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), $"ID : {data.GetMainID()}");
            EditorGUI.LabelField(new Rect(rect.x, rect.y + 20, rect.width, 20), $"Name : {data.Name}");

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (Index_ == index)
                {
                    Index_ = -1;
                }
                else
                {
                    Index_ = index;
                }
            }
        }

        private void DrawInspector(Rect rect)
        {
            EditorGUILayout.LabelField("Inspector");
            if (Index_ < 0 || Index_ >= DataList_.Count)
            {
                return;
            }

            InspectorScrollPos_ = EditorGUILayout.BeginScrollView(InspectorScrollPos_);
            
            EditorGUI.BeginChangeCheck();
            DataList_[Index_] = (T)ViewUtils.DrawData("Inspector", DataList_[Index_], typeof(T));
            if (EditorGUI.EndChangeCheck())
            {
                IsDirty_ = true;
            }
            
            EditorGUILayout.EndScrollView();
        }

        public T[] GetData()
        {
            return DataList_.ToArray();
        }

        private void CreateItem()
        {
            var data = TypeUtils.CreateInstance<T>();
            DataList_.Add(data);
            Index_ = DataList_.IndexOf(data);
            IsDirty_ = true;
        }

        private void DeleteItem(int index)
        {
            DataList_.RemoveAt(index);
            Index_ = -1;
            IsDirty_ = true;
        }

        private void DuplicateItem(int index)
        {
            var item = (T)DataList_[index].Clone();
            DataList_.Add(item);
            Index_ = DataList_.IndexOf(item);
            IsDirty_ = true;
        }

        private void SortDataList()
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
            IsDirty_ = true;
        }

        public void Load()
        {
            // Debug.Log($"read {Name} data");
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonFile_);
            var data = JsonUtils.DecodeArray(text.text, typeof(T)).Cast<T>().ToArray();
            DataList_.Clear();
            DataList_.AddRange(data);
            SortDataList();
        }

        public void Save()
        {
            if (IsDirty_)
            {
                var jsonText = JsonUtils.EncodeArray(DataList_.ToArray());
                var dir = Path.GetDirectoryName(JsonFile_);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(JsonFile_, jsonText);
                IsDirty_ = false;
            }
        }
    }
}