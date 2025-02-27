#if UNITY_EDITOR
using System.Collections.Generic;
using LiteQuark.Editor;
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
            if (string.IsNullOrWhiteSpace(CurrentStateGroup_))
            {
                return new List<string>();
            }

            var timelinePathList = AssetUtils.GetAssetPathList("TimelineAsset", GetCurrentUnitTimelineRootPath());
            var stateNameList = new List<string>();

            foreach (var timelinePath in timelinePathList)
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }

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
#endif