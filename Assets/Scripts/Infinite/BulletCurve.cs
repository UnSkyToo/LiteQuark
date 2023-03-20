using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace InfiniteGame
{
    public class BulletCurve : MonoBehaviour
    {
        public bool IsAlive { get; private set; }
        
        public GameObjectPool Pool;
        public float Velocity;
        
        public CurveData Curve;
        
        private IBezierCurve[] Curves_;
        private float CurveCurTime_;
        private float CurveMaxTime_;
        
        private void Start()
        {
            IsAlive = true;
            CurveMaxTime_ = CurveLength() / Velocity;
            CurveCurTime_ = 0;
            
            Curves_ = new IBezierCurve[Curve.Points.Length / 4];
            for (var index = 0; index < Curves_.Length; ++index)
            {
                Curves_[index] = BezierCurveFactory.CreateBezierCurve(
                    Curve.Points[index * 4 + 0],
                    Curve.Points[index * 4 + 1],
                    Curve.Points[index * 4 + 2],
                    Curve.Points[index * 4 + 3]);
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }
            
            transform.localPosition = CurveLerp(CurveCurTime_, CurveMaxTime_);
            CurveCurTime_ += Time.fixedDeltaTime;
            if (CurveCurTime_ >= CurveMaxTime_)
            {
                if (Curve.Loop)
                {
                    CurveCurTime_ -= CurveMaxTime_;
                }
                else
                {
                    Dead();
                }
            }
        }

        private void Update()
        {
        }

        private void Dead()
        {
            IsAlive = false;
            Pool.Recycle(gameObject);
            DestroyImmediate(this);
        }
        
        private float CurveLength()
        {
            var total = 0f;
            
            for (var index = 0; index < Curve.Points.Length - 1; ++index)
            {
                var len = Vector2.Distance(Curve.Points[index + 0], Curve.Points[index + 1]);
                total += len;
            }

            return total;
        }
        
        private Vector2 CurveLerp(float time, float max)
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
    }
}