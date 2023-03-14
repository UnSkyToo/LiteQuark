using UnityEngine;

namespace Platform
{
    public class MovingObject : PhysicsObject
    {
        private readonly GameObject Go_;

        protected Vector2 OldPosition_;
        protected Vector2 Position_;
        protected Vector2 OldSpeed_;
        protected Vector2 Speed_;
        
        public MovingObject(PhysicsWorld world, GameObject go, AABB aabb)
            : base(world, aabb)
        {
            Go_ = go;

            Position_ = Vector2.zero;
            Speed_ = Vector2.zero;
        }

        public override void Update(float time)
        {
            OldPosition_ = Position_;
            OldSpeed_ = Speed_;
            
            Position_ += Speed_ * time;
            Bounds_.Center = Position_;
            Go_.transform.localPosition = new Vector3(Position_.x, Position_.y, 0);
        }

        public void SetPosition(Vector2 position)
        {
            Position_ = position;
        }

        // public void OnCollisionEnter(MovingObject other)
        // {
        // }
        //
        // public void OnCollisionStay(MovingObject other)
        // {
        // }
        //
        // public void OnCollisionExit(MovingObject other)
        // {
        // }
    }
}