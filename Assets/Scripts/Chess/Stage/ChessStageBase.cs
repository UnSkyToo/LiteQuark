using UnityEngine;

namespace LiteGamePlay.Chess
{
    public abstract class ChessStageBase
    {
        public abstract string ViewName { get; }
        public Transform View { get; set; }
        
        public virtual void OnOpen(params object[] args)
        {
        }

        public virtual void OnClose()
        {
        }

        public virtual void OnTick(float deltaTime)
        {
        }

        protected Transform GetChild(string path)
        {
            return View.Find(path);
        }

        protected T GetComponent<T>(string path) where T : Component
        {
            var child = GetChild(path);
            if (child == null)
            {
                Debug.LogError($"can't get child : {path}");
                return default;
            }
            
            return child.GetComponent<T>();
        }
    }
}