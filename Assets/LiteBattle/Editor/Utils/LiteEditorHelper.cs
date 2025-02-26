using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using LiteConst = LiteBattle.Runtime.LiteConst;
using LiteLabelAttribute = LiteBattle.Runtime.LiteLabelAttribute;

namespace LiteBattle.Editor
{
    public static class LiteEditorHelper
    {
        public static List<string> GetLiteStateNameList()
        {
            var stateNameList = new List<string>();

            foreach (var timelinePath in LiteStateEditor.Instance.GetCurrentAgentTimelinePathList())
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }

            return stateNameList;
        }

        public static List<string> GetAnimationNameList()
        {
            if (LiteEditorBinder.Instance.IsBindAgent())
            {
                return LiteEditorBinder.Instance.GetAnimatorStateNameList();
            }

            return new List<string>();
        }

        public static List<string> GetInputKeyList()
        {
            return LiteConst.KeyName.GetKeyList();
        }
    }
}