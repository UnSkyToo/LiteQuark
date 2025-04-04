﻿using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public class LiteRuntimeView : LiteBaseView
    {
        private readonly LiteRuntimeUnitList List_;
        private bool IsVisible_;
        
        public LiteRuntimeView(LiteNexusEditor nexusEditor)
            : base(nexusEditor)
        {
            List_ = new LiteRuntimeUnitList(nexusEditor);
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
                    LiteEditorBinder.Instance.UnBindUnit();
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
            var unit = List_.GetSelectItem();
            if (unit == null)
            {
                return;
            }

            var state = unit.GetStateMachine()?.GetCurrentState();
            if (state == null)
            {
                return;
            }
            
            var currentTime = state.GetCurrentTime();
            NexusEditor.GetTimelineCtrlView().SetFrameIndex(LiteTimelineHelper.TimeToFrame(currentTime));

            if (TimelineEditor.inspectedAsset != null && TimelineEditor.inspectedAsset.name == state.Name)
            {
                return;
            }

            var stateGroup = unit.GetUnitConfig().StateGroup;
            var timelineFullPath = PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), $"{stateGroup}/{state.Name}.playable");
            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineFullPath);
            LiteTimelineHelper.SetTimeline(asset);
        }

        private class LiteRuntimeUnitList : LiteListView<LiteUnit>
        {
            private readonly LiteNexusEditor NexusEditor_;
            
            public LiteRuntimeUnitList(LiteNexusEditor nexusEditor)
            {
                NexusEditor_ = nexusEditor;
                HideToolbar = true;
            }
            
            protected override List<LiteUnit> GetList()
            {
                var entityList = LiteEntityManager.Instance.GetEntityList();
                var result = new List<LiteUnit>();
                foreach (var entity in entityList)
                {
                    if (entity is LiteUnit unit)
                    {
                        result.Add(unit);
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

            protected override bool DrawItem(int index, bool selected, LiteUnit obj)
            {
                var config = obj.GetUnitConfig();

                if (GUILayout.Button($"{config.name}({obj.UniqueID})", selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
                {
                    selected = !selected;
                    
                    LiteEditorBinder.Instance.BindUnitRuntime(obj);
                    NexusEditor_.GetTimelineView().Rebind();
                }
                
                return selected;
            }
        }
    }
}