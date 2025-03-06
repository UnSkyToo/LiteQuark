using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public static class LiteUnitBinderDataForEditor
    {
        private static string CurrentStateGroup_;
        private static List<string> AnimatorStateNameList_ = new List<string>();

        public static void SetCurrentStateGroup(string stateGroup)
        {
            CurrentStateGroup_ = stateGroup;
        }
        
        public static string GetCurrentUnitTimelineRootPath()
        {
            return PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), CurrentStateGroup_);
        }
        
        public static List<string> GetCurrentUnitTimelinePathList()
        {
            var stateNameList = new List<string>();
            
            if (string.IsNullOrWhiteSpace(CurrentStateGroup_))
            {
                return stateNameList;
            }
            
#if UNITY_EDITOR
            var timelinePathList = LiteQuark.Editor.AssetUtils.GetAssetPathList("TimelineAsset", GetCurrentUnitTimelineRootPath());
            foreach (var timelinePath in timelinePathList)
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }
#endif

            return stateNameList;
        }

        public static void SetAnimatorStateNameList(List<string> animatorStateNameList)
        {
            AnimatorStateNameList_ = animatorStateNameList ?? new List<string>();
        }

        public static List<string> GetAnimatorStateNameList()
        {
            return AnimatorStateNameList_;
        }
    }
}