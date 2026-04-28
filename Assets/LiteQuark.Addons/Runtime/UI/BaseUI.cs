using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseUI : BaseObject
    {
        public override string DebugName => $"UI<{Config.DepthMode},{State}> - {Config.PrefabPath}";

        public UIConfig Config { get; set; }
        public UISystem System { get; set; }
        public UIState State { get; set; }
        
        public GameObject Go { get; private set; }
        public RectTransform RT { get; private set; }

        public int SortingOrder { get; private set; }

        private GameObjectHandle _goHandle;
        private readonly List<AssetHandle<Sprite>> _spriteHandleList = new List<AssetHandle<Sprite>>();
        private readonly int _eventTag;

        protected BaseUI()
            : base()
        {
            _eventTag = (int)UniqueID;
        }

        public void BindGo(GameObject go)
        {
            BindGo(go, null);
        }

        public void BindGo(GameObject go, GameObjectHandle handle)
        {
            _goHandle = handle;
            Go = go;
            RT = Go.GetComponent<RectTransform>();
            SortingOrder = Go.GetComponent<Canvas>().sortingOrder;
        }

        internal void ReleaseGo()
        {
            if (_goHandle != null)
            {
                _goHandle.Dispose();
                _goHandle = null;
            }
            else if (Go != null)
            {
                UnityEngine.Object.Destroy(Go);
            }

            Go = null;
            RT = null;
        }

        public void Open(params object[] paramList)
        {
            AdaptAnchorsValue();
            OnOpen(paramList);
        }

        public void Close()
        {
            UnloadSprites();
            UnRegisterAllEvent();
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
            if (!Config.AutoAdapt)
            {
                return;
            }
            
            var maxWidth = Display.main.systemWidth;
            var maxHeight = Display.main.systemHeight;
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= maxWidth;
            anchorMin.y /= maxHeight;
            anchorMax.x /= maxWidth;
            anchorMax.y /= maxHeight;

            var canvasSize = System.CanvasRoot.sizeDelta;

            OnAdaptAnchorsValue(canvasSize, anchorMin, anchorMax);
        }

        protected virtual void OnAdaptAnchorsValue(Vector2 canvasSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            RT.anchorMin = anchorMin;
            RT.anchorMax = anchorMax;
        }

        public Transform FindChild(string path)
        {
            return UnityUtils.FindChild(Go, path);
        }

        public Component GetComponent(string path, Type type)
        {
            return UnityUtils.GetComponent(Go, path, type);
        }
        
        public Component GetComponent(Type type)
        {
            return Go.GetComponent(type);
        }
        
        public T GetComponent<T>(string path) where T : Component
        {
            return UnityUtils.GetComponent<T>(Go, path);
        }
        
        public T GetComponent<T>() where T : Component
        {
            return Go.GetComponent<T>();
        }

        public void SetActive(string path, bool value)
        {
            UnityUtils.SetActive(Go, path, value);
        }
        
        public void LoadSprite(string resPath, Action<Sprite> callback)
        {
            var handle = LiteRuntime.Asset.LoadAssetHandle<Sprite>(resPath);
            _spriteHandleList.Add(handle);
            LoadSpriteAsync(handle, callback).Forget();
        }

        private async UniTaskVoid LoadSpriteAsync(AssetHandle<Sprite> handle, Action<Sprite> callback)
        {
            try
            {
                await handle;
                if (_spriteHandleList.Contains(handle))
                {
                    LiteUtils.SafeInvoke(callback, handle.Result);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_spriteHandleList.Remove(handle))
                {
                    handle.Dispose();
                }

                LLog.Error("Load sprite error: {0}", ex.Message);
            }
        }
        
        public void ReplaceSprite(string path, string resPath)
        {
            LoadSprite(resPath, (sprite) =>
            {
                UIUtils.ReplaceSprite(Go, path, sprite);
            });
        }
        
        public void ReplaceSprite(Transform parent, string path, string resPath)
        {
            LoadSprite(resPath, (sprite) =>
            {
                UIUtils.ReplaceSprite(parent, path, sprite);
            });
        }
        
        public void ReplaceSprite(GameObject parent, string path, string resPath)
        {
            ReplaceSprite(parent.transform, path, resPath);
        }

        private void UnloadSprites()
        {
            foreach (var handle in _spriteHandleList)
            {
                handle.Dispose();
            }
            _spriteHandleList.Clear();
        }

        /// <summary>
        /// 注册事件，UI会在关闭后自动反注册
        /// </summary>
        protected void RegisterEvent<T>(Action<T> callback) where T : IEventData
        {
            LiteRuntime.Event.Register(_eventTag, callback);
        }

        /// <summary>
        /// 手动反注册事件
        /// </summary>
        protected void UnRegisterEvent<T>(Action<T> callback) where T : IEventData
        {
            LiteRuntime.Event.UnRegister(_eventTag, callback);
        }
        
        private void UnRegisterAllEvent()
        {
            LiteRuntime.Event.UnRegisterAll(_eventTag);
        }
    }
}
