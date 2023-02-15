using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public abstract class ClassifyDataView<T> : DataViewBase<T> where T : IJsonMainConfig
    {
        protected ClassifyDataView(string name, string jsonFile)
            : base(name, jsonFile)
        {
        }
        
        protected override void DrawDataList(Rect rect)
        {
            var height = DrawClassifyToolbar();
            
            var listRect = new Rect(rect.x, rect.y + height, rect.width, rect.height - height);
            DrawClassifyList(listRect, GetSelectList());
        }

        protected virtual float DrawClassifyToolbar()
        {
            return 0;
        }

        protected abstract T[] GetSelectList();
        
        protected void DrawClassifyList(Rect rect, T[] list)
        {
            var itemWidth = GetItemWidth();
            var itemHeight = GetItemHeight();
            
            var xCount = Mathf.FloorToInt(rect.width / itemWidth);
            var yCount = Mathf.CeilToInt((float)DataList_.Count / (float)xCount);
            var height = Mathf.Max(rect.height, yCount * itemHeight);
            
            var dataRect = new Rect(rect.x, rect.y + 20, rect.width, rect.height - 20);
            DataScrollPos_ = GUI.BeginScrollView(dataRect, DataScrollPos_, new Rect(dataRect.x, dataRect.y, dataRect.width, height));

            for (var index = 0; index < list.Length; ++index)
            {
                var y = index / xCount;
                var x = index % xCount;
                var itemRect = new Rect(dataRect.x + x * itemWidth, dataRect.y + y * itemHeight, itemWidth, itemHeight);
                DrawDataItem(itemRect, list[index]);
            }
            
            GUI.EndScrollView();
        }

        protected virtual float GetItemWidth()
        {
            return 100;
        }

        protected virtual float GetItemHeight()
        {
            return 40;
        }
        
        private void DrawDataItem(Rect rect, T data)
        {
            var index = DataList_.IndexOf(data);

            OnDrawDataItem(rect, data, SelectIndex_ == index);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (SelectIndex_ == index)
                {
                    SelectIndex_ = -1;
                }
                else
                {
                    SelectIndex_ = index;
                }
            }
        }

        protected virtual void OnDrawDataItem(Rect rect, T data, bool isSelected)
        {
            if (isSelected)
            {
                using (new LiteQuark.Editor.ColorScope(Color.red))
                {
                    GUI.Box(rect, Texture2D.redTexture);
                }
            }
            
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), $"ID : {data.GetMainID()}");
            EditorGUI.LabelField(new Rect(rect.x, rect.y + 20, rect.width, 20), $"Name : {data.Name}");
        }

        protected override void OnDataChange()
        {
            RebuildClassifyList();
        }

        protected abstract void RebuildClassifyList();
    }
}