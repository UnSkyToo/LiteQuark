using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public class LiteLogicEntryData
    {
        [SerializeField]
        public bool Disabled;
        [SerializeField]
        public string AssemblyQualifiedName;

        public LiteLogicEntryData()
        {
        }
    }
}