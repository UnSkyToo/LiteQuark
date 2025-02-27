using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public class LiteRuntimeView : LiteViewBase
    {
        private readonly LiteRuntimeAgentList List_;
        private bool IsVisible_;
        
        public LiteRuntimeView(LiteStateEditor stateEditor)
            : base(stateEditor)
        {
            List_ = new LiteRuntimeAgentList(stateEditor);
            IsVisible_ = false;
        }

        public override void Dispose()
        {
        }

        public override bool IsVisible()
        {
            return IsVisible_;
        }

        public override void OnPlayModeStateChange(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    IsVisible_ = true;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    LiteEditorBinder.Instance.UnBindAgent();
                    IsVisible_ = false;
                    break;
            }
        }

        public override void Draw()
        {
            DrawTitle("Runtime View");
            
            List_.RefreshData();
            List_.Draw();
            
            // UpdateTimelineState();
        }

        private void UpdateTimelineState()
        {
            var agent = List_.GetSelectItem();
            if (agent == null)
            {
                return;
            }

            var state = agent.GetStateMachine().GetCurrentState();
            if (state == null)
            {
                return;
            }
            
            var currentTime = state.GetCurrentTime();
            StateEditor.GetTimelineCtrlView().SetFrameIndex(LiteTimelineHelper.TimeToFrame(currentTime));

            if (TimelineEditor.inspectedAsset != null && TimelineEditor.inspectedAsset.name == state.Name)
            {
                return;
            }

            var stateGroup = agent.GetAgentConfig().StateGroup;
            var timelineFullPath = PathUtils.ConcatPath(LiteStateConfig.Instance.GetTimelineDatabasePath(), $"{stateGroup}/{state.Name}.playable");
            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineFullPath);
            LiteTimelineHelper.SetTimeline(asset);
        }

        private class LiteRuntimeAgentList : LiteListView<LiteAgent>
        {
            private readonly LiteStateEditor StateEditor_;
            
            public LiteRuntimeAgentList(LiteStateEditor stateEditor)
            {
                StateEditor_ = stateEditor;
                HideToolbar = true;
            }
            
            protected override List<LiteAgent> GetList()
            {
                var entityList = LiteEntityMgr.Instance.GetEntityList();
                var result = new List<LiteAgent>();
                foreach (var entity in entityList)
                {
                    if (entity is LiteAgent agent)
                    {
                        result.Add(agent);
                    }
                }
                return result;
            }

            protected override bool CreateItem()
            {
                return false;
            }

            protected override bool DeleteItem(int index)
            {
                return false;
            }

            protected override bool ChangeItem(int index)
            {
                return false;
            }

            protected override void SwapItem(int index1, int index2)
            {
            }

            protected override bool DrawItem(int index, bool selected, LiteAgent obj)
            {
                var config = obj.GetAgentConfig();

                if (GUILayout.Button($"{config.name}({obj.UniqueID})", selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
                {
                    selected = !selected;
                    
                    LiteEditorBinder.Instance.BindAgentRuntime(obj);
                    StateEditor_.GetTimelineView().Rebind();
                }
                
                return selected;
            }
        }
    }
}