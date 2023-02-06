using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UIArrowItem : IDisposable
    {
        private GameObject Go_;
        
        public UIArrowItem(Transform parent, string path)
        {
            LiteRuntime.Get<AssetSystem>().LoadGameObject(path, (go) =>
            {
                UnityUtils.SetParent(parent, go);
                Go_ = go;
            });
        }

        public void Dispose()
        {
            LiteRuntime.Get<AssetSystem>().UnloadGameObject(Go_);
        }

        public void RefreshInfo(Vector2 begin, Vector2 end)
        {
            var dist = Vector2.Distance(begin, end);
            
            var transform = Go_.GetComponent<RectTransform>();
            transform.anchoredPosition = begin;
            transform.sizeDelta = new Vector2(15f, dist);

            var from = Vector2.up;
            var to = (end - begin).normalized;
            var angle = MathUtils.Angle(from, to);

            transform.rotation = Quaternion.AngleAxis(angle, Vector3.back);
        }
    }
}