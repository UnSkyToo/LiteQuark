using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay.Chess
{
    public sealed class ChessStageManager : Singleton<ChessStageManager>
    {
        private Transform Root_;
        private ChessStageBase CurrentStage_;
        
        public void ChangeTo<T>(params object[] args) where T : ChessStageBase
        {
            CloseStage(CurrentStage_);
            CurrentStage_ = OpenStage<T>(args);
        }

        public void ChangeToEmpty()
        {
            CloseStage(CurrentStage_);
            CurrentStage_ = null;
        }
        
        public void Tick(float deltaTime)
        {
            CurrentStage_?.OnTick(deltaTime);
        }

        private ChessStageBase OpenStage<T>(params object[] args) where T : ChessStageBase
        {
            if (Root_ == null)
            {
                Root_ = GameObject.Find("PanelGame").transform;
            }

            var stage = Activator.CreateInstance<T>();
            if (stage == null)
            {
                Debug.LogError($"cant create {typeof(T)} stage");
                return null;
            }
            
            stage.View = Root_.Find(stage.ViewName);
            
            if (stage.View != null)
            {
                stage.View.gameObject.SetActive(true);
            }

            stage.OnOpen(args);
            return stage;
        }

        private void CloseStage(ChessStageBase stage)
        {
            if (stage == null)
            {
                return;
            }

            if (stage.View != null)
            {
                stage.View.gameObject.SetActive(false);
            }
            
            stage.OnClose();
        }
    }
}