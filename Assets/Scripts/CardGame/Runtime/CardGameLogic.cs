using LiteCard.UI;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard
{
    public sealed class CardGameLogic : ILogic, IOnGUI
    {
        public bool Startup()
        {
            // GameLogic.Instance.Startup();
            LiteRuntime.Get<UISystem>().OpenUI<UILoading>();
            return true;
        }

        public void Shutdown()
        {
            Preloader.Instance.Unload();
            GameLogic.Instance.Shutdown();
            Resources.UnloadUnusedAssets();
        }

        public void Tick(float deltaTime)
        {
            GameLogic.Instance.Update(deltaTime);
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Test", GUILayout.Width(200), GUILayout.Height(80)))
            {
                GameLogic.Instance.Startup();
            }
        }
    }
}