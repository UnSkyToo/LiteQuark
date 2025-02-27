using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public class LiteEditorBinder : Singleton<LiteEditorBinder>
    {
        public bool IsReady => IsBindAgent() && IsBindTimeline();
        
        private LiteAgentConfig CurrentAgent_ = null;
        private GameObject AgentGo_ = null;

        private PlayableDirector TimelineDirector_;
        private TimelineAsset CurrentTimeline_ = null;

        private ILiteRange CurrentRange_ = null;

        private LiteEditorBinder()
        {
        }

        public void Startup()
        {
            TimelineDirector_ = GameObject.Find("TimelineViewer").GetComponent<PlayableDirector>();
        }

        public void Shutdown()
        {
            UnBindAgent();
            UnBindTimeline();
            UnBindAttackRange();
            Selection.activeObject = null;
        }

        public bool IsBindAgent()
        {
            return CurrentAgent_ != null;
        }

        public LiteAgentConfig GetAgent()
        {
            return CurrentAgent_;
        }
        
        public string GetCurrentStateGroup()
        {
            if (!IsBindAgent())
            {
                return string.Empty;
            }

            return CurrentAgent_.StateGroup;
        }

        public List<string> GetAnimatorStateNameList()
        {
            return LiteAgentBinder.Instance.GetAnimatorStateNameList();
        }

        public AnimatorState GetAnimatorState(string stateName)
        {
            return LiteAgentBinder.Instance.GetAnimatorState(stateName);
        }

        public float GetAnimatorStateLength(string stateName)
        {
            return LiteAgentBinder.Instance.GetAnimatorStateLength(stateName);
        }
        
        public string GetCurrentAgentTimelineRootPath()
        {
            var stateGroup = GetCurrentStateGroup();
            return PathUtils.ConcatPath(LiteStateConfig.Instance.GetTimelineDatabasePath(), stateGroup);
        }
        
        public List<string> GetCurrentAgentTimelinePathList()
        {
            if (string.IsNullOrWhiteSpace(GetCurrentStateGroup()))
            {
                return new List<string>();
            }
            
            return AssetUtils.GetAssetPathList("TimelineAsset", GetCurrentAgentTimelineRootPath());
        }
        
        public void BindAgent(LiteAgentConfig config)
        {
            UnBindAgent();
            CurrentAgent_ = config;
            Selection.activeObject = config;

            var prefabFullPath = PathUtils.GetFullPathInAssetRoot(config.PrefabPath);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFullPath);
            if (go == null)
            {
                LLog.Error($"can't load agent prefab : {prefabFullPath}");
                return;
            }

            AgentGo_ = Object.Instantiate(go, Vector3.zero, Quaternion.identity);
            AgentGo_.name = config.name;
            LiteAgentBinder.Instance.Bind(config, AgentGo_);
        }

        public void BindAgentRuntime(LiteAgent agent)
        {
            UnBindAgent();
            CurrentAgent_ = agent.GetAgentConfig();
        }

        public void UnBindAgent()
        {
            UnBindTimeline();
            
            if (CurrentAgent_ != null)
            {
                CurrentAgent_ = null;
                LiteAgentBinder.Instance.UnBind();
            }

            if (AgentGo_ != null)
            {
                Object.DestroyImmediate(AgentGo_);
                AgentGo_ = null;
            }

            Selection.activeObject = null;
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

        public bool IsBindAttackRange()
        {
            return CurrentRange_ != null;
        }

        public ILiteRange GetAttackRange()
        {
            return CurrentRange_;
        }

        public void BindAttackRange(ILiteRange range)
        {
            CurrentRange_ = range;
        }

        public void UnBindAttackRange()
        {
            CurrentRange_ = null;
        }
    }
}