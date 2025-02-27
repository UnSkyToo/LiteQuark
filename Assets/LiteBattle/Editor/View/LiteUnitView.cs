using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public sealed class LiteUnitView : LiteBaseView
    {
        private readonly LiteUnitConfigList List_;

        public LiteUnitView(LiteNexusEditor nexusEditor)
            : base(nexusEditor)
        {
            List_ = new LiteUnitConfigList(nexusEditor);
            List_.RefreshData();
        }

        public override void Dispose()
        {
        }

        public override bool IsVisible()
        {
            return NexusEditor.IsEditorMode();
        }

        public override void OnPlayModeStateChange(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    LiteEditorBinder.Instance.UnBindUnit();
                    break;
                default:
                    break;
            }
        }
        
        public override void Draw()
        {
            DrawTitle("Unit View");
            
            List_.Draw();
        }

        private class LiteUnitConfigList : LiteListView<string>
        {
            private readonly LiteNexusEditor NexusEditor_;
            
            public LiteUnitConfigList(LiteNexusEditor nexusEditor)
            {
                NexusEditor_ = nexusEditor;
                EnableOrderControl = false;
            }
            
            protected override List<string> GetList()
            {
                var results = AssetUtils.GetAssetPathList("LiteUnitConfig", LiteNexusConfig.Instance.GetUnitDatabasePath());
                return results;
            }

            protected override bool CreateItem()
            {
                AssetUtils.CreateAsset<LiteUnitConfig>(LiteNexusConfig.Instance.GetUnitDatabasePath(), $"{AssetUtils.RandomAssetName("unit_")}.asset");
                return true;
            }

            protected override bool DeleteItem(int index)
            {
                var val = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(val);
                if (LiteEditorUtils.ShowConfirmDialog($"Delete Unit {name}"))
                {
                    LiteEditorBinder.Instance.UnBindUnit();
                    // LiteAssetHelper.DeleteAsset(val);
                    LiteUnitHelper.DeleteAsset(val);
                    OnDataChanged();
                    return true;
                }

                return false;
            }

            protected override bool ChangeItem(int index)
            {
                var unitConfigFullPath = GetItem(index);
                var name = PathUtils.GetFileNameWithoutExt(unitConfigFullPath);
                LiteInputTextDialog.ShowDialog("Change Name", name, (newName) =>
                {
                    AssetUtils.RenameAsset(unitConfigFullPath, newName);
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
                var unitFullPath = obj;
                var name = PathUtils.GetFileNameWithoutExt(unitFullPath);
                
                if (GUILayout.Button(name, selected ? LiteEditorStyle.ButtonSelect : LiteEditorStyle.ButtonNormal, GUILayout.ExpandWidth(true)))
                {
                    selected = !selected;
                    
                    LiteEditorBinder.Instance.UnBindUnit();
                    if (selected)
                    {
                        var config = AssetDatabase.LoadAssetAtPath<LiteUnitConfig>(unitFullPath);
                        LiteEditorBinder.Instance.BindUnit(config);
                        NexusEditor_.GetTimelineView().Rebind();
                    }
                }

                return selected;
            }
        }
    }
}