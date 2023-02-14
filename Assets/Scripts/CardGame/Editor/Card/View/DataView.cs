using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public class DataView<T> : DataViewBase<T> where T : IJsonMainConfig
    {
        public const float ItemWidth = 100f;
        public const float ItemHeight = 40f;

        public DataView(string name, string jsonFile)
            : base(name, jsonFile)
        {
        }

        protected override void DrawDataList(Rect rect)
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

            if (SelectIndex_ == index)
            {
                using (new LiteQuark.Editor.ColorScope(Color.red))
                {
                    GUI.Box(rect, Texture2D.redTexture);
                }
            }
            
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), $"ID : {data.GetMainID()}");
            EditorGUI.LabelField(new Rect(rect.x, rect.y + 20, rect.width, 20), $"Name : {data.Name}");

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
    }
}