using System;
using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    [Serializable]
    public struct LiteGroupData
    {
        public string Name { get; private set; }
        public string AgentPath { get; private set; }
        public List<string> TimelineList { get; private set; }
        
        public LiteGroupData(string name, string agentPath, List<string> timelineList)
        {
            Name = name;
            AgentPath = agentPath;
            TimelineList = timelineList;
        }
    }
}