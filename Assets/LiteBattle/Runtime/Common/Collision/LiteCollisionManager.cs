using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteCollisionManager : Singleton<LiteCollisionManager>
    {
        public const int MaxOverlapCount = 20;
        
        private Collider[] CollideResult_ = new Collider[MaxOverlapCount];

        private LiteCollisionManager()
        {
        }

        public List<LiteEntity> CheckCollide(LiteEntity owner, ILiteRange range, LiteEntityCamp camp)
        {
            var results = new List<LiteEntity>();

            var count = CheckOverlap(owner, range);
            for (var i = 0; i < count; ++i)
            {
                if (CollideResult_[i].gameObject.GetInstanceID() == owner.GetInstanceID())
                {
                    continue;
                }
                
                var collider = CollideResult_[i].GetComponent<LiteColliderBinder>();
                if (collider != null)
                {
                    var entity = LiteEntityManager.Instance.GetEntity(collider.EntityUniqueID);
                    if (entity != null && CheckCamp(entity, camp))
                    {
                        results.Add(entity);
                    }
                }
            }

            return results;
        }

        private int CheckOverlap(LiteEntity owner, ILiteRange range)
        {   
            switch (range)
            {
                case LiteBoxRange boxRange:
                    return Physics.OverlapBoxNonAlloc(owner.Position + owner.Rotation * boxRange.Offset, boxRange.Size * 0.5f, CollideResult_, owner.Rotation);
                case LiteSphereRange sphereRange:
                    return Physics.OverlapSphereNonAlloc(owner.Position + owner.Rotation * sphereRange.Offset, sphereRange.Radius, CollideResult_);
                default:
                    break;
            }

            return 0;
        }

        private bool CheckCamp(LiteEntity entity, LiteEntityCamp checkCamp)
        {
            if (checkCamp == LiteEntityCamp.All)
            {
                return true;
            }

            return entity.Camp == checkCamp;
        }
    }
}