using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteBattle.Editor
{
    public static class LiteEditorHelper
    {
        public static List<string> GetLiteStateNameList()
        {
            var stateNameList = new List<string>();

            foreach (var timelinePath in LiteEditorBinder.Instance.GetCurrentAgentTimelinePathList())
            {
                stateNameList.Add(PathUtils.GetFileNameWithoutExt(timelinePath));
            }

            return stateNameList;
        }
    }
}