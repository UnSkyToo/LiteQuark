using UnityEngine;

namespace Rogue
{
    public class BloodHud : MonoBehaviour
    {
        public Camera LookAt;
        
        private void LateUpdate()
        {
            transform.LookAt(transform.position + LookAt.transform.forward);
        }
    }
}