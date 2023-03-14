using System.Collections.Generic;

namespace Platform
{
    public class PhysicsObject
    {
        private static int IDGenerator_ = 1;
        public int UniqueID { get; }
        
        public PhysicsWorld World { get; }

        protected AABB Bounds_;

        private List<string> Tags_;

        protected PhysicsObject(PhysicsWorld world, AABB bounds)
        {
            UniqueID = IDGenerator_++;
            World = world;
            Bounds_ = bounds;

            Tags_ = new List<string>();
        }
        
        public bool Overlaps(PhysicsObject other)
        {
            return Bounds_.Overlaps(other.Bounds_);
        }

        public virtual void Update(float time)
        {
        }

        public void AddTag(string tag)
        {
            Tags_.Add(tag);
        }

        public bool ContainsTag(string tag)
        {
            return Tags_.Contains(tag);
        }
    }
}