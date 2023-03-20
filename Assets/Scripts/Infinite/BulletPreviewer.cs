using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class BulletPreviewer : MonoBehaviour
    {
        [Header("角度")]
        public float Angular;
        public float AngularAcceleration;
        public float AngularAccelerationDelay;

        [Header("速度")]
        public float Velocity;
        public float Acceleration;
        public float AccelerationDelay;

        public CurveData Curve;

        private GameObjectPool CurvePool_;

        private void Start()
        {
            CurvePool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Infinite/Prefab/BulletCurve.prefab");
        }

        public void Fire()
        {
            if (Curve != null)
            {
                var bullet = CreateBulletCurve();
                bullet.Velocity = Velocity;
                bullet.Curve = Curve;
            }
            else
            {
                var bullet = BulletManager.Instance.CreateBullet(Vector3.zero);
                bullet.Angular = Angular;
                bullet.AngularAcceleration = AngularAcceleration;
                bullet.AngularAccelerationDelay = AngularAccelerationDelay;
                bullet.Velocity = Velocity;
                bullet.Acceleration = Acceleration;
                bullet.AccelerationDelay = AccelerationDelay;
            }
        }

        private BulletCurve CreateBulletCurve()
        {
            var go = CurvePool_.Alloc();
            var ctrl = go.GetOrAddComponent<BulletCurve>();
            ctrl.Pool = CurvePool_;
            
            var beginPos = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                beginPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Curve != null)
            {
                beginPos = Curve.Points[0];
            }
            
            go.transform.localPosition = new Vector3(beginPos.x, beginPos.y, 0);
            go.transform.localScale = Vector3.one * 0.5f;
            go.tag = Const.Tag.Bullet;

            return ctrl;
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(BulletPreviewer))]
    public sealed class BulletPreviewerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Fire"))
            {
                var previewer = target as BulletPreviewer;
                previewer.Fire();
            }
        }
    }
#endif
}