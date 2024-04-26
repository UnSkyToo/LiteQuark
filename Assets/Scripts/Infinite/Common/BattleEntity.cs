using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public abstract class BattleEntity : BaseObject, ITick
    {
        public GameObject Go { get; }
        
        private CircleArea Circle_;
        private Transform Transform_;
        private Vector3 Position_;

        protected BattleEntity(GameObject go, CircleArea circle)
        {
            Go = go;
            Transform_ = go.transform;

            Circle_ = circle;
        }
        
        public void SetPosition(Vector3 position)
        {
            Position_ = position;
            Transform_.localPosition = Position_;
            Circle_.Move(position);
        }

        public CircleArea GetCircle()
        {
            return Circle_;
        }

        public Vector3 GetPosition()
        {
            return Position_;
        }

        public abstract void Tick(float deltaTime);

        public void DrawGizmo()
        {
        }
    }
}