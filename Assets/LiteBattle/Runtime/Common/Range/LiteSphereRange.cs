using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    [LiteLabel("Sphere")]
    [Serializable]
    public class LiteSphereRange : ILiteRange
    {
        [LiteProperty("偏移", LitePropertyType.Vector3)]
        public Vector3 Offset = Vector3.zero;
        
        [LiteProperty("半径", LitePropertyType.Float)]
        public float Radius = 1f;
        
        public bool HasData => true;

        public ILiteRange Clone()
        {
            var range = new LiteSphereRange();
            range.Offset = Offset;
            range.Radius = Radius;
            return range;
        }
    }
}