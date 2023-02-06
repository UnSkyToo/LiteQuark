using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard.UI
{
    public abstract class UIItemBase<T> : IDisposable
    {   
        public GameObject Go { get; }
        public T Data { get; }

        public event Action<UIItemBase<T>> ItemChange; 

        protected UIItemBase(GameObject go, T data)
        {
            Go = go;
            Data = data;
        }

        public virtual void Dispose()
        {
            LiteRuntime.Get<AssetSystem>().UnloadGameObject(Go);
        }

        public abstract void RefreshInfo();

        protected void OnItemChange()
        {
            ItemChange?.Invoke(this);
        }
    }

    public class UIItemList<TItem, TData> : IDisposable where TItem : UIItemBase<TData>
    {
        public event Action<int, TItem> ItemCreate;
        public event Action<TItem> ItemChange; 

        private readonly Transform Content_;
        private readonly string PrefabPath_;
        private readonly List<TItem> ItemList_;

        public UIItemList(Transform content, string prefabPath)
        {
            Content_ = content;
            PrefabPath_ = prefabPath;
            ItemList_ = new List<TItem>();
        }

        public void Dispose()
        {
            ClearItemList();
        }

        public TItem[] GetItemList()
        {
            return ItemList_.ToArray();
        }

        public void RefreshInfo(TData[] dataList)
        {
            ClearItemList();
            CreateItemList(dataList);
        }

        private void ClearItemList()
        {
            foreach (var item in ItemList_)
            {
                item.ItemChange -= OnItemChange;
                item.Dispose();
            }
            ItemList_.Clear();
        }
        
        private void CreateItemList(TData[] dataList)
        {
            foreach (var data in dataList)
            {
                CreateItem(data);
            }
        }
        
        private void CreateItem(TData data)
        {
            LiteRuntime.Get<AssetSystem>().LoadGameObject(PrefabPath_, (instance) =>
            {
                instance.transform.SetParent(Content_, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localScale = Vector3.one;
                var item = Activator.CreateInstance(typeof(TItem), instance, data) as TItem;
                item.RefreshInfo();
                ItemList_.Add(item);
                
                ItemCreate?.Invoke(ItemList_.Count - 1, item);
                item.ItemChange += OnItemChange;
            });
        }

        private void OnItemChange(UIItemBase<TData> item)
        {
            ItemChange?.Invoke(item as TItem);
        }
    }
}