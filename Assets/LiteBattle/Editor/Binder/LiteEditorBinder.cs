using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public class LiteEditorBinder : Singleton<LiteEditorBinder>
    {
        public bool IsReady => IsBindUnit() && IsBindTimeline();

        private LiteUnitBinder UnitBinder_;
        private LiteTimelineBinder TimelineBinder_;
        private LiteRangeBinder RangeBinder_;

        private LiteEditorBinder()
        {
        }

        public void Startup()
        {
            UnitBinder_ = new LiteUnitBinder();
            TimelineBinder_ = new LiteTimelineBinder();
            RangeBinder_ = new LiteRangeBinder();
        }

        public void Shutdown()
        {
            if (UnitBinder_ != null)
            {
                UnitBinder_.Dispose();
                UnitBinder_ = null;
            }
            
            if (TimelineBinder_ != null)
            {
                TimelineBinder_.Dispose();
                TimelineBinder_ = null;
            }
            
            if (RangeBinder_ != null)
            {
                RangeBinder_.Dispose();
                RangeBinder_ = null;
            }
            
            Selection.activeObject = null;
        }

        public bool IsBindUnit()
        {
            return UnitBinder_?.IsBindUnit() ?? false;
        }

        public LiteUnitConfig GetUnit()
        {
            return UnitBinder_?.GetUnit();
        }

        public GameObject GetUnitGo()
        {
            return UnitBinder_?.GetUnitGo();
        }
        
        public string GetCurrentStateGroup()
        {
            return UnitBinder_.GetCurrentStateGroup();
        }

        public List<string> GetAnimatorStateNameList()
        {
            return UnitBinder_.GetAnimatorStateNameList();
        }

        public AnimatorState GetAnimatorState(string stateName)
        {
            return UnitBinder_.GetAnimatorState(stateName);
        }

        public float GetAnimatorStateLength(string stateName)
        {
            return UnitBinder_.GetAnimatorStateLength(stateName);
        }

        public void SampleAnimation(string animatorStateName, float time)
        {
            UnitBinder_?.SampleAnimation(animatorStateName, time);
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
            UnitBinder_.BindUnit(config);
        }

        public void BindUnitRuntime(LiteUnit unit)
        {
            UnitBinder_?.BindUnitRuntime(unit);
        }

        public void UnBindUnit()
        {
            UnBindTimeline();
            UnitBinder_?.UnBindUnit();
            Selection.activeObject = null;
        }

        public bool IsBindTimeline()
        {
            return TimelineBinder_?.IsBindTimeline() ?? false;
        }

        public TimelineAsset GetTimeline()
        {
            return TimelineBinder_?.GetTimeline();
        }

        public void BindTimeline(TimelineAsset asset)
        {
            TimelineBinder_?.BindTimeline(asset);
        }

        public void UnBindTimeline()
        {
            TimelineBinder_?.UnBindTimeline();
        }

        public bool IsBindAttackRange()
        {
            return RangeBinder_?.IsBindAttackRange() ?? false;
        }

        public ILiteRange GetAttackRange()
        {
            return RangeBinder_?.GetAttackRange();
        }

        public void BindAttackRange(ILiteRange range)
        {
            RangeBinder_?.BindAttackRange(range);
        }

        public void UnBindAttackRange()
        {
            RangeBinder_?.UnBindAttackRange();
        }
    }
}