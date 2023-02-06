using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard.UI
{
    public abstract class UIBase
    {
        public abstract string PrefabPath { get; }
        public GameObject Go { get; private set; }

        protected UIBase()
        {
        }

        public void BindGo(GameObject go)
        {
            Go = go;
        }

        public void Open(params object[] paramList)
        {
            AdaptAnchorsValue();
            OnOpen(paramList);
        }

        public void Close()
        {
            OnClose();
        }

        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
        }
        
        protected virtual void OnOpen(params object[] paramList)
        {
        }
        
        protected virtual void OnClose()
        {
        }

        protected virtual void OnUpdate(float deltaTime)
        {
        }

        private void AdaptAnchorsValue()
        {
            var maxWidth = Display.main.systemWidth;
            var maxHeight = Display.main.systemHeight;
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= maxWidth;
            anchorMin.y /= maxHeight;
            anchorMax.x /= maxWidth;
            anchorMax.y /= maxHeight;

            Go.GetComponent<RectTransform>().anchorMin = anchorMin;
            Go.GetComponent<RectTransform>().anchorMax = anchorMax;
        }
    }
}