using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public sealed class LiteTimelineView : LiteViewBase
    {
        private readonly LiteTimelineAssetList List_;

        public LiteTimelineView(LiteStateEditor stateEditor)
            : base(stateEditor)
        {
            List_ = new LiteTimelineAssetList(stateEditor);
        }

        public override void Dispose()
        {
        }

        public override bool IsVisible()
        {
            return LiteEditorBinder.Instance.IsBindAgent();
        }

        public override void Draw()
        {
            DrawTitle($"Timeline View : {LiteEditorBinder.Instance.GetCurrentStateGroup()}");

            List_.Draw();
        }

        public void Rebind()
        {
            LiteEditorBinder.Instance.UnBindTimeline();
            List_.Reset();
            List_.RefreshData();
        }

        private class LiteTimelineAssetList : LiteListView<string>
        {
            private readonly LiteStateEditor StateEditor_;
            
            public LiteTimelineAssetList(LiteStateEditor stateEditor)
            {
                StateEditor_ = stateEditor;
                EnableOrderControl = false;
            }
            
            protected override List<string> GetList()
            {
                var results = StateEditor_.GetCurrentAgentTimelinePathList();
                return results;
            }

            protected override bool CreateItem()
            {
                LiteAssetHelper.CreateAsset<TimelineAsset>(StateEditor_.GetCurrentAgentTimelineRootPath(), $"{LiteAssetHelper.RandomAssetName("timeline_")}.playable");
                return true;
            }

            protected override bool DeleteItem(int index)
            {
                var val = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(val);
                if (LiteEditorHelper.ShowConfirmDialog($"Delete Timeline {name}"))
                {
                    LiteEditorBinder.Instance.UnBindTimeline();
                    LiteAssetHelper.DeleteAsset(val);
                    OnDataChanged();
                    return true;
                }

                return false;
            }

            protected override bool ChangeItem(int index)
            {
                var timelineFullPath = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(timelineFullPath);
                LiteInputTextDialog.ShowDialog("Change Name", name, (newName) =>
                {
                    LiteAssetHelper.RenameAsset(timelineFullPath, newName);
                    OnDataChanged();
                });
                
                return false;
            }

            protected override void SwapItem(int index1, int index2)
            {
                var temp = GetItem(index1);
                SetItem(index1, GetItem(index2));
                SetItem(index2, temp);
            }

            protected override bool DrawItem(int index, bool selected, string obj)
            {
                var timelineFullPath = obj;
                var name = PathUtils.GetFileNameWithoutExt(timelineFullPath);

                if (GUILayout.Button(name, selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
                {
                    selected = !selected;
                    
                    LiteEditorBinder.Instance.UnBindTimeline();
                    if (selected)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineFullPath);
                        LiteEditorBinder.Instance.BindTimeline(asset);
                    }
                }
                
                if (selected && GUILayout.Button(GUIContent.none, LiteEditorStyle.DropdownOption))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Duplicate"), false, () =>
                    {
                        LiteAssetHelper.DuplicateAsset(timelineFullPath);
                        OnDataChanged();
                    });

                    menu.ShowAsContext();
                }

                return selected;
            }
        }
    }
}