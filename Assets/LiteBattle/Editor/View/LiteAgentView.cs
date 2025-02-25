using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public sealed class LiteAgentView : LiteViewBase
    {
        private readonly LiteAgentConfigList List_;

        public LiteAgentView(LiteStateEditor stateEditor)
            : base(stateEditor)
        {
            List_ = new LiteAgentConfigList(stateEditor);
            List_.RefreshData();
        }

        public override void Dispose()
        {
        }

        public override bool IsVisible()
        {
            return StateEditor.IsEditorMode();
        }

        public override void OnPlayModeStateChange(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    LiteEditorBinder.Instance.UnBindAgent();
                    break;
                default:
                    break;
            }
        }
        
        public override void Draw()
        {
            DrawTitle("Agent View");
            
            List_.Draw();
        }

        private class LiteAgentConfigList : LiteListView<string>
        {
            private readonly LiteStateEditor StateEditor_;
            
            public LiteAgentConfigList(LiteStateEditor stateEditor)
            {
                StateEditor_ = stateEditor;
                EnableOrderControl = false;
            }
            
            protected override List<string> GetList()
            {
                var results = LiteAssetHelper.GetAssetPathList("LiteAgentConfig", LiteStateUtils.GetAgentRootPath());
                return results;
            }

            protected override bool CreateItem()
            {
                LiteAssetHelper.CreateAsset<LiteAgentConfig>(LiteStateUtils.GetAgentRootPath(), $"{LiteAssetHelper.RandomAssetName("agent_")}.asset");
                return true;
            }

            protected override bool DeleteItem(int index)
            {
                var val = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(val);
                if (LiteEditorHelper.ShowConfirmDialog($"Delete Agent {name}"))
                {
                    LiteEditorBinder.Instance.UnBindAgent();
                    // LiteAssetHelper.DeleteAsset(val);
                    LiteAgentHelper.DeleteAsset(val);
                    OnDataChanged();
                    return true;
                }

                return false;
            }

            protected override bool ChangeItem(int index)
            {
                var agentConfigFullPath = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(agentConfigFullPath);
                LiteInputTextDialog.ShowDialog("Change Name", name, (newName) =>
                {
                    LiteAssetHelper.RenameAsset(agentConfigFullPath, newName);
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
                var agentFullPath = obj;
                var name = PathUtils.GetFileNameWithoutExt(agentFullPath);
                
                if (GUILayout.Button(name, selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
                {
                    selected = !selected;
                    
                    LiteEditorBinder.Instance.UnBindAgent();
                    if (selected)
                    {
                        var config = AssetDatabase.LoadAssetAtPath<LiteAgentConfig>(agentFullPath);
                        LiteEditorBinder.Instance.BindAgent(config);
                        StateEditor_.GetTimelineView().Rebind();
                    }
                }

                return selected;
            }
        }
    }
}