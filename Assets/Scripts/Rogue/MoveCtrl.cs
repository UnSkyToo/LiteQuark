using UnityEngine;

namespace Rogue
{
    public class MoveCtrl : MonoBehaviour
    {
        public float RotateSpeed;
        public float WalkSpeed;
        public float RunSpeed;
        public float PushForce;
        
        private CharacterController Controller_;
        private Quaternion TargetRotate_;

        private void Awake()
        {
            Controller_ = GetComponent<CharacterController>();
            TargetRotate_ = transform.rotation;
        }

        private void Update()
        {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            if (transform.rotation != TargetRotate_)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotate_, RotateSpeed);
            }

            var moveDir = new Vector3(h, 0, v).normalized;
            var moveSpeed = Input.GetAxis("Fire3") > 0 ? RunSpeed : WalkSpeed;
            
            Controller_.SimpleMove(moveSpeed * moveDir);

            if (moveDir.sqrMagnitude > 0)
            {
                TargetRotate_ = Quaternion.LookRotation(moveDir, transform.up);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.transform.CompareTag("Player"))
            {
                var rigidbody = hit.collider.attachedRigidbody;
                if (rigidbody != null && !rigidbody.isKinematic)
                {
                    rigidbody.velocity = hit.moveDirection * PushForce;
                }
            }
        }
    }
}