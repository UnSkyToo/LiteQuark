using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletCurve : BulletBase
    {
        private CurveData Curve_;
        private IBezierCurve[] Curves_;
        private float CurveCurTime_;
        private float CurveMaxTime_;
        private float Velocity_;

        private Vector3 Offset_;
        
        public BulletCurve(GameObject go, CircleArea circle, CurveData curve, float velocity)
            : base(go, circle)
        {
            Curve_ = curve;
            Velocity_ = velocity;
            
            CurveMaxTime_ = CurveLength() / Velocity_;
            CurveCurTime_ = 0;
            
            Curves_ = new IBezierCurve[Curve_.Points.Length / 4];
            for (var index = 0; index < Curves_.Length; ++index)
            {
                Curves_[index] = BezierCurveFactory.CreateBezierCurve(
                    Curve_.Points[index * 4 + 0],
                    Curve_.Points[index * 4 + 1],
                    Curve_.Points[index * 4 + 2],
                    Curve_.Points[index * 4 + 3]);
            }

            Offset_ = Vector3.zero;
            DoLerp();
        }

        public float GetMaxLerpTime()
        {
            return CurveMaxTime_;
        }

        public void SetLerpTime(float lerpTime)
        {
            CurveCurTime_ = lerpTime;
            DoLerp();
        }

        public void SetOffset(Vector3 offset)
        {
            Offset_ = offset;
        }

        private void DoLerp()
        {
            var position = CurveLerp(CurveCurTime_, CurveMaxTime_) + Offset_;
            SetPosition(position);
        }

        public override void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }
            
            DoLerp();
            CheckCollision();
            
            CurveCurTime_ += deltaTime;
            if (CurveCurTime_ >= CurveMaxTime_)
            {
                if (Curve_.Loop)
                {
                    CurveCurTime_ -= CurveMaxTime_;
                }
                else
                {
                    Dead();
                }
            }
        }
        
        private float CurveLength()
        {
            var total = 0f;
            
            for (var index = 0; index < Curve_.Points.Length - 1; ++index)
            {
                var len = Vector2.Distance(Curve_.Points[index + 0], Curve_.Points[index + 1]);
                total += len;
            }

            return total;
        }
        
        private Vector3 CurveLerp(float time, float max)
        {
            if (Curves_.Length == 1)
            {
                return Curves_[0].Lerp(time / max);
            }
            else
            {
                var subMax = max / (float)Curves_.Length;
                var subTime = time % subMax;
                var index = (int)(time / subMax);
                return Curves_[index].Lerp(subTime / subMax);
            }
        }

        private void CheckCollision()
        {
            var result = PhysicUtils.CheckOverlapEnemy(GetCircle());
            foreach (var enemy in result)
            {
                enemy.OnBulletCollision(this);
            }
        }
    }
}