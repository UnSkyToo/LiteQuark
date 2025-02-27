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
        public bool IsReady => IsBindUnit() && IsBindTimeline();
        
        private LiteUnitConfig CurrentUnit_ = null;
        private GameObject UnitGo_ = null;

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
            UnBindUnit();
            UnBindTimeline();
            UnBindAttackRange();
            Selection.activeObject = null;
        }

        public bool IsBindUnit()
        {
            return CurrentUnit_ != null;
        }

        public LiteUnitConfig GetUnit()
        {
            return CurrentUnit_;
        }
        
        public string GetCurrentStateGroup()
        {
            if (!IsBindUnit())
            {
                return string.Empty;
            }

            return CurrentUnit_.StateGroup;
        }

        public List<string> GetAnimatorStateNameList()
        {
            return LiteUnitBinder.Instance.GetAnimatorStateNameList();
        }

        public AnimatorState GetAnimatorState(string stateName)
        {
            return LiteUnitBinder.Instance.GetAnimatorState(stateName);
        }

        public float GetAnimatorStateLength(string stateName)
        {
            return LiteUnitBinder.Instance.GetAnimatorStateLength(stateName);
        }
        
        public string GetCurrentUnitTimelineRootPath()
        {
            var stateGroup = GetCurrentStateGroup();
            return PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), stateGroup);
        }
        
        public List<string> GetCurrentUnitTimelinePathList()
        {
            if (string.IsNullOrWhiteSpace(GetCurrentStateGroup()))
            {
                return new List<string>();
            }
            
            return AssetUtils.GetAssetPathList("TimelineAsset", GetCurrentUnitTimelineRootPath());
        }
        
        public void BindUnit(LiteUnitConfig config)
        {
            UnBindUnit();
            CurrentUnit_ = config;
            Selection.activeObject = config;

            var prefabFullPath = PathUtils.GetFullPathInAssetRoot(config.PrefabPath);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFullPath);
            if (go == null)
            {
                LLog.Error($"can't load unit prefab : {prefabFullPath}");
                return;
            }

            UnitGo_ = Object.Instantiate(go, Vector3.zero, Quaternion.identity);
            UnitGo_.name = config.name;
            LiteUnitBinder.Instance.Bind(config, UnitGo_);
        }

        public void BindUnitRuntime(LiteUnit unit)
        {
            UnBindUnit();
            CurrentUnit_ = unit.GetUnitConfig();
        }

        public void UnBindUnit()
        {
            UnBindTimeline();
            
            if (CurrentUnit_ != null)
            {
                CurrentUnit_ = null;
                LiteUnitBinder.Instance.UnBind();
            }

            if (UnitGo_ != null)
            {
                Object.DestroyImmediate(UnitGo_);
                UnitGo_ = null;
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