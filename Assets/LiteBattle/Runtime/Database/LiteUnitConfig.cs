using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteUnitConfig : ScriptableObject
    {
        [LiteProperty("Prefab", LitePropertyType.GameObject)]
        public string PrefabPath;
        [LiteProperty("State Group", LitePropertyType.String)]
        public string StateGroup;
        [LiteProperty("Entry State", LitePropertyType.String)]
        public string EntryState;
    }
}