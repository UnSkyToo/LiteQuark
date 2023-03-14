using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    public class PhysicsWorld : IDisposable
    {
        public int Count => ObjectList_.Count;

        // private readonly GameObject Root_;
        private readonly List<PhysicsObject> ObjectList_ = new List<PhysicsObject>();
        private readonly Map Map_;

        public PhysicsWorld()
        {
            // Root_ = new GameObject("World");
            Map_ = new Map(Screen.width / Const.TileSizeX, Screen.height / Const.TileSizeY);
        }

        public void Dispose()
        {
            Map_.Dispose();
            // GameObject.Destroy(Root_);
        }

        public Map GetMap()
        {
            return Map_;
        }

        public void Update(float time)
        {
            foreach (var obj in ObjectList_)
            {
                obj.Update(time);
            }
        }

        public bool Overlaps(PhysicsObject obj)
        {
            foreach (var other in ObjectList_)
            {
                if (other.UniqueID == obj.UniqueID)
                {
                    continue;
                }
                
                if (obj.Overlaps(other))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddObject(PhysicsObject obj)
        {
            ObjectList_.Add(obj);
        }
    }
}