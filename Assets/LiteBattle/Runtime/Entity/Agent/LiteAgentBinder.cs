﻿using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteAgentBinder : MonoBehaviour
    {
        private Animator[] Animators_;
        private readonly List<string> AnimatorStateNameList_ = new List<string>();
        private readonly Dictionary<string, AnimatorState> AnimatorStateList_ = new Dictionary<string, AnimatorState>();

        private int PreviewAnimatorStateIndex_ = -1;
        private float PreviewAnimatorStateTime_ = 0f;
        
        public void Initialize()
        {
            AnimatorStateNameList_.Clear();
            AnimatorStateList_.Clear();

            PreviewAnimatorStateIndex_ = -1;
            PreviewAnimatorStateTime_ = 0f;
            
            Animators_ = GetComponentsInChildren<Animator>();
            
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
        
        public List<string> GetAnimatorStateNameList()
        {
            return AnimatorStateNameList_;
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
                return 0;
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
                clip.SampleAnimation(gameObject, time);
            }
        }
    }
}