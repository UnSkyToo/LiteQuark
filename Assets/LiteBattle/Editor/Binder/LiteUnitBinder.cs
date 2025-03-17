using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LiteBattle.Editor
{
    internal class LiteUnitBinder : IDispose
    {
        private bool IsRuntime_ = false;
        private LiteUnitConfig CurrentUnit_ = null;
        private GameObject UnitGo_ = null;
        private Animator[] Animators_;
        private readonly List<string> AnimatorStateNameList_ = new List<string>();
        private readonly Dictionary<string, AnimatorState> AnimatorStateList_ = new Dictionary<string, AnimatorState>();
        private readonly Dictionary<string, EffectBinder> EffectList_ = new Dictionary<string, EffectBinder>();

        private int PreviewAnimatorStateIndex_ = -1;
        private float PreviewAnimatorStateTime_ = 0f;
        
        public LiteUnitBinder()
        {
        }

        public void Dispose()
        {
            UnBindUnit();
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
            GenerateAnimatorData(UnitGo_);
            
            LiteUnitBinderDataForEditor.SetCurrentStateGroup(CurrentUnit_.StateGroup);
            LiteUnitBinderDataForEditor.SetAnimatorStateNameList(AnimatorStateNameList_);
        }

        public void BindUnitRuntime(LiteUnit unit)
        {
            UnBindUnit();
            CurrentUnit_ = unit.GetUnitConfig();
            UnitGo_ = unit.GetModule<LiteEntityBehaveModule>().GetInternalGo();
            IsRuntime_ = true;
            GenerateAnimatorData(UnitGo_);
            
            LiteUnitBinderDataForEditor.SetCurrentStateGroup(CurrentUnit_.StateGroup);
            LiteUnitBinderDataForEditor.SetAnimatorStateNameList(AnimatorStateNameList_);
        }

        private void GenerateAnimatorData(GameObject go)
        {
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

        public void UnBindUnit()
        {
            if (!IsRuntime_ && UnitGo_ != null)
            {
                Object.DestroyImmediate(UnitGo_);
                UnitGo_ = null;
            }
            
            IsRuntime_ = false;
            CurrentUnit_ = null;
            Animators_ = null;
            AnimatorStateNameList_.Clear();
            AnimatorStateList_.Clear();

            if (EffectList_.Count > 0)
            {
                foreach (var effect in EffectList_)
                {
                    effect.Value.Stop();
                    Object.DestroyImmediate(effect.Value.gameObject);
                }
                EffectList_.Clear();
            }

            LiteUnitBinderDataForEditor.SetCurrentStateGroup(string.Empty);
            LiteUnitBinderDataForEditor.SetAnimatorStateNameList(null);
        }

        public bool IsBindUnit()
        {
            return CurrentUnit_ != null;
        }

        public LiteUnitConfig GetUnit()
        {
            return CurrentUnit_;
        }

        public GameObject GetUnitGo()
        {
            return UnitGo_;
        }
        
        public string GetCurrentStateGroup()
        {
            if (!IsBindUnit())
            {
                return string.Empty;
            }

            return CurrentUnit_.StateGroup;
        }
        
        public string GetCurrentUnitTimelineRootPath()
        {
            var stateGroup = GetCurrentStateGroup();
            return PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), stateGroup);
        }
        
        public string GetAnimatorStateName(int index)
        {
            if (index < 0 || index >= AnimatorStateNameList_.Count)
            {
                return string.Empty;
            }

            return AnimatorStateNameList_[index];
        }

        public List<string> GetAnimatorStateNameList()
        {
            return AnimatorStateNameList_;
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
                clip.SampleAnimation(UnitGo_, time);
            }
        }

        private EffectBinder GetEffect(string effectPath)
        {
            if (string.IsNullOrWhiteSpace(effectPath))
            {
                return null;
            }
            
            if (!EffectList_.ContainsKey(effectPath))
            {
                var effectFullPath = PathUtils.GetFullPathInAssetRoot(effectPath);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(effectFullPath);
                if (go == null)
                {
                    LLog.Error($"can't load effect prefab : {effectFullPath}");
                    return null;
                }
                
                var effectGo = Object.Instantiate(go, Vector3.zero, Quaternion.identity);
                if (effectGo == null)
                {
                    LLog.Error($"can't load effect prefab : {effectFullPath}");
                    return null;
                }

                effectGo.hideFlags = HideFlags.DontSave;

                var effect = effectGo.GetComponent<EffectBinder>();
                if (effect == null)
                {
                    effect = effectGo.AddComponent<EffectBinder>();
                    effect.UpdateInfo();
                }
                
                EffectList_.Add(effectPath, effect);
            }
            
            return EffectList_[effectPath];
        }

        public void StopEffect(string effectPath)
        {
            if (string.IsNullOrWhiteSpace(effectPath))
            {
                return;
            }
            
            if (EffectList_.ContainsKey(effectPath))
            {
                var effect = EffectList_[effectPath];
                effect.Stop();
                Object.DestroyImmediate(effect.gameObject);
                EffectList_.Remove(effectPath);
            }
        }

        public float GetEffectLength(string effectPath)
        {
            var effect = GetEffect(effectPath);
            return effect?.LifeTime ?? 0f;
        }

        public void SampleEffect(string effectPath, Vector3 effectPosition, float time)
        {
            var effect = GetEffect(effectPath);
            effect.transform.localPosition = effectPosition;
            effect?.SetTime(time);
        }
    }
}