using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    internal class LiteTimelineBinder : IDispose
    {
        private GameObject TempGo_;
        private PlayableDirector TimelineDirector_;
        private TimelineAsset CurrentTimeline_ = null;

        public LiteTimelineBinder()
        {
            var go = GameObject.Find("TimelineViewer");
            if (go == null)
            {
                go = new GameObject("TimelineViewer");
                go.AddComponent<PlayableDirector>();
                TempGo_ = go;
            }
            
            TimelineDirector_ = go.GetComponent<PlayableDirector>();
        }

        public void Dispose()
        {
            UnBindTimeline();
            
            if (TempGo_ != null)
            {
                Object.DestroyImmediate(TempGo_);
                TempGo_ = null;
            }
            
            TimelineDirector_ = null;
        }
        
        public bool IsBindTimeline()
        {
            return CurrentTimeline_ != null;
        }

        public TimelineAsset GetTimeline()
        {
            return CurrentTimeline_;
        }

        public void BindTimeline(TimelineAsset asset)
        {
            UnBindTimeline();
            CurrentTimeline_ = asset;
            LiteTimelineHelper.SetTimeline(asset);
            if (TimelineDirector_ != null)
            {
                TimelineDirector_.playableAsset = asset;
                Selection.activeObject = TimelineDirector_.gameObject;
            }
        }

        public void UnBindTimeline()
        {
            if (CurrentTimeline_ != null)
            {
                CurrentTimeline_ = null;
                LiteTimelineHelper.ClearTimeline();
            }

            if (TimelineDirector_ != null)
            {
                TimelineDirector_.playableAsset = null;
            }
        }
    }
}