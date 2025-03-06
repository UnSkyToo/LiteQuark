using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public class LiteSystemEntryData
    {
        [SerializeField]
        public bool Disabled;
        [SerializeField]
        public string AssemblyQualifiedName;

        public LiteSystemEntryData()
        {
        }
    }
}