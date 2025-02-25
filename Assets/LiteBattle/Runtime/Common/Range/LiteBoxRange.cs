using System;
using UnityEngine;

namespace LiteBattle.Runtime
{
    [LiteLabel("Box")]
    [Serializable]
    public class LiteBoxRange : ILiteRange
    {
        [LiteProperty("偏移", LitePropertyType.Vector3)]
        public Vector3 Offset = Vector3.zero;
        
        [LiteProperty("大小", LitePropertyType.Vector3)]
        public Vector3 Size = Vector3.one;
        
        public bool HasData => true;

        public ILiteRange Clone()
        {
            var range = new LiteBoxRange();
            range.Offset = Offset;
            range.Size = Size;
            return range;
        }
    }
}