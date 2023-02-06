using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public class ClassifyDataView<T> : DataView<T> where T : IJsonMainConfig
    {
        private static readonly Dictionary<string, bool> FoldoutCache_ = new Dictionary<string, bool>();
        private readonly Func<T, string> ClassifyGetter_;

        public ClassifyDataView(string name, int priority, string jsonFile, Func<T, string> classifyGetter)
            : base(name, priority, jsonFile)
        {
            ClassifyGetter_ = classifyGetter;
        }
        
        protected override void DrawDataList(Rect rect)
        {
            var classifyList = GetDataClassify();
            var xCount = Mathf.FloorToInt(rect.width / ItemWidth);
            var height = CalcDataListHeight(classifyList, xCount);

            var dataRect = new Rect(rect.x, rect.y + 20, rect.width, rect.height - 20);
            height = Mathf.Max(height, dataRect.height);
            
            DataScrollPos_ = GUI.BeginScrollView(dataRect, DataScrollPos_, new Rect(dataRect.x, dataRect.y, dataRect.width, height));

            var startX = dataRect.x;
            var startY = dataRect.y;
            
            foreach (var classify in classifyList)
            {
                using (new LiteEditor.ColorScope(Color.green))
                {
                    FoldoutCache_[classify.Key] = EditorGUI.Foldout(new Rect(startX, startY, dataRect.width, 20), FoldoutCache_[classify.Key], classify.Key);
                }
                
                startY += 20;
                
                if (FoldoutCache_[classify.Key])
                {
                    startY = DrawClassifyList(xCount, startX, startY, classify.Value);
                }
            }
            
            GUI.EndScrollView();
        }

        private float CalcDataListHeight(Dictionary<string, List<T>> classifyList, int xCount)
        {
            var height = 0f;
            
            foreach (var classify in classifyList)
            {
                if (!FoldoutCache_.ContainsKey(classify.Key))
                {
                    FoldoutCache_.Add(classify.Key, true);
                }

                if (FoldoutCache_[classify.Key])
                {
                    height = height + Mathf.CeilToInt(classify.Value.Count / (float)xCount) * ItemHeight + 20;
                }
                else
                {
                    height = height + 20;
                }
            }

            return height;
        }

        private float DrawClassifyList(int xCount, float startX, float startY, List<T> dataList)
        {
            for (var index = 0; index < dataList.Count; ++index)
            {
                var y = index / xCount;
                var x = index % xCount;
                var itemRect = new Rect(startX + x * ItemWidth, startY + y * ItemHeight, ItemWidth, ItemHeight);
                // DrawDataItem(itemRect, index);
                DrawDataItem(itemRect, DataList_.IndexOf(dataList[index]));
            }

            return startY + Mathf.CeilToInt(dataList.Count / (float)xCount) * ItemHeight;
        }

        private Dictionary<string, List<T>> GetDataClassify()
        {
            var dataList = GetData();
            var result = new Dictionary<string, List<T>>();

            foreach (var data in dataList)
            {
                var classifyName = ClassifyGetter_.Invoke(data);
                if (!result.ContainsKey(classifyName))
                {
                    result.Add(classifyName, new List<T>());
                }
                result[classifyName].Add(data);
            }
            
            return result;
        }
    }
}