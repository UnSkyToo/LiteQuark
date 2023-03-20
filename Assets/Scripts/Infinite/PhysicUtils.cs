using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteGame
{
    public static class PhysicUtils
    {
        private static readonly Collider2D[] Result_ = new Collider2D[128];
        
        public static Collider2D CheckOverlapCircleOne(Vector2 position, float radius, int layerMask, string tag = null)
        {
            if (Physics2D.OverlapCircleNonAlloc(position, radius, Result_, layerMask) > 0)
            {
                if (tag == null || Result_[0].CompareTag(tag))
                {
                    return Result_[0];
                }
            }

            return null;
        }

        public static Collider2D[] CheckOverlapCircle(Vector2 position, float radius, int layerMask, string tag = null)
        {
            var count = Physics2D.OverlapCircleNonAlloc(position, radius, Result_, layerMask);
            if (count == 0)
            {
                return Array.Empty<Collider2D>();
            }

            var result = new List<Collider2D>();

            for (var index = 0; index < count; ++index)
            {
                if (tag == null || Result_[index].CompareTag(tag))
                {
                    result.Add(Result_[index]);
                }
            }

            return result.ToArray();
        }
    }
}