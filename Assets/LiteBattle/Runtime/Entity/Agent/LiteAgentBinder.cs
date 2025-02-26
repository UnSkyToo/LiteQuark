#if UNITY_EDITOR
using System.Collections.Generic;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor.Animations;
using UnityEngine;

namespace LiteBattle.Runtime
{
    // TODO : replace LiteEditorBinder with LiteAgentBinder ???
    public sealed class LiteAgentBinder : Singleton<LiteAgentBinder>
    {
        private LiteAgentConfig CurrentAgent_;
        private GameObject GameObject_;
        private Animator[] Animators_;
        private readonly List<string> AnimatorStateNameList_ = new List<string>();
        private readonly Dictionary<string, AnimatorState> AnimatorStateList_ = new Dictionary<string, AnimatorState>();

        private int PreviewAnimatorStateIndex_ = -1;
        private float PreviewAnimatorStateTime_ = 0f;

        private LiteAgentBinder()
        {
        }
        
        public void Bind(LiteAgentConfig config, GameObject go)
        {
            UnBind();
            
            CurrentAgent_ = config;
            GameObject_ = go;
            AnimatorStateNameList_.Clear();
            AnimatorStateList_.Clear();

            PreviewAnimatorStateIndex_ = -1;
            PreviewAnimatorStateTime_ = 0f;
            
            Animators_ = go.GetComponentsInChildren<Animator>();
            
            foreach (var animator in Animators_)
            {
                var ac = animator.runtimeAnimatorController as AnimatorController;
                if (ac == null)
                {
                    continue;
                }

                var stateList = ac.layers[0].stateMachine.states;
                foreach (var state in stateList)
                {
                    AnimatorStateNameList_.Add(state.state.name);
                    AnimatorStateList_.Add(state.state.name, state.state);
                }
            }
        }
        
        public void UnBind()
        {
            GameObject_ = null;
            Animators_ = null;
            AnimatorStateNameList_.Clear();
            AnimatorStateList_.Clear();
        }
        
        public bool IsBindAgent()
        {
            return CurrentAgent_ != null;
        }

        public LiteAgentConfig GetAgent()
        {
            return CurrentAgent_;
        }

        public GameObject GetAgentGo()
        {
            return GameObject_;
        }
        
        public string GetCurrentStateGroup()
        {
            if (!IsBindAgent())
            {
                return string.Empty;
            }

            return CurrentAgent_.StateGroup;
        }
        
        public string GetCurrentAgentTimelineRootPath()
        {
            var stateGroup = GetCurrentStateGroup();
            // TODO : use config path
            return PathUtils.ConcatPath("Assets/StandaloneAssets/demo/StateData/Timeline", stateGroup);
        }
        
        public static List<string> GetCurrentAgentTimelinePathListForAttribute()
        {
            if (string.IsNullOrWhiteSpace(Instance.GetCurrentStateGroup()))
            {
                return new List<string>();
            }
            
            var timelinePathList = AssetUtils.GetAssetPathList("TimelineAsset", Instance.GetCurrentAgentTimelineRootPath());
            var stateNameList = new List<string>();

            foreach (var timelinePath in timelinePathList)
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }

            return stateNameList;
        }
        
        public List<string> GetAnimatorStateNameList()
        {
            return AnimatorStateNameList_;
        }

        public static List<string> GetAnimatorStateNameListForAttribute()
        {
            return Instance.GetAnimatorStateNameList();
        }

        public string GetAnimatorStateName(int index)
        {
            if (index < 0 || index >= AnimatorStateNameList_.Count)
            {
                return string.Empty;
            }

            return AnimatorStateNameList_[index];
        }
        
        public AnimatorState GetAnimatorState(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return null;
            }
            
            if (AnimatorStateList_.ContainsKey(stateName))
            {
                return AnimatorStateList_[stateName];
            }

            return null;
        }
        
        public float GetAnimatorStateLength(string stateName)
        {
            var state = GetAnimatorState(stateName);
            if (state == null)
            {
                return 0f;
            }

            return state.motion.averageDuration;
        }

        public float GetAnimatorStateLength(int index)
        {
            var stateName = GetAnimatorStateName(index);
            return GetAnimatorStateLength(stateName);
        }

        public int GetPreviewAnimatorStateIndex()
        {
            return PreviewAnimatorStateIndex_;
        }

        public void SetPreviewAnimatorStateIndex(int index)
        {
            if (PreviewAnimatorStateIndex_ == index)
            {
                return;
            }

            PreviewAnimatorStateIndex_ = index;
            PreviewAnimatorStateTime_ = 0f;
            SampleAnimation();
        }

        public float GetPreviewAnimatorStateTime()
        {
            return PreviewAnimatorStateTime_;
        }

        public void SetPreviewAnimatorStateTime(float time)
        {
            PreviewAnimatorStateTime_ = time;
            SampleAnimation();
        }

        private void SampleAnimation()
        {
            var stateName = GetAnimatorStateName(PreviewAnimatorStateIndex_);
            SampleAnimation(stateName, PreviewAnimatorStateTime_);
        }

        public void SampleAnimation(string animatorStateName, float time)
        {
            var state = GetAnimatorState(animatorStateName);
            
            if (state == null)
            {
                return;
            }

            if (state.motion is AnimationClip clip)
            {
                clip.SampleAnimation(GameObject_, time);
            }
        }
    }
}
#endif