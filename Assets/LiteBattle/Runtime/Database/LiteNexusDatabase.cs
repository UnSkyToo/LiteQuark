using System;
using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    [Serializable]
    public struct LiteNexusDatabase
    {
        public List<string> UnitList { get; private set; }
        public Dictionary<string, List<string>> StateMap { get; private set; }
        
        public LiteNexusDatabase(List<string> unitList, Dictionary<string, List<string>> stateMap)
        {
            UnitList = unitList;
            StateMap = stateMap;
        }
    }
}