using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public class LiteTypeEntryData<T>
    {
        [SerializeField]
        public bool Disabled;
        [SerializeField]
        public string AssemblyQualifiedName;
        [HideInInspector] public string BaseTypeName;
        
        public LiteTypeEntryData()
        {
            BaseTypeName = typeof(T).AssemblyQualifiedName;
        }
    }
}