using UnityEngine;

namespace UzbCarParking.VehiclePhysics
{
    /// <summary>
    /// Stabilizator (anti-roll bar): bitta o‘qdagi ikki g‘ildirak osmasining siqilish farqiga
    /// qarab kuch beradi va burilishda kuzov og‘ishini kamaytiradi.
    /// </summary>
    public class AntiRollBar : MonoBehaviour
    {
        public WheelCollider left;
        public WheelCollider right;
        public float stiffness = 5000f;

        private Rigidbody body;

        public void Setup(WheelCollider leftWheel, WheelCollider rightWheel, float barStiffness)
        {
            left = leftWheel;
            right = rightWheel;
            stiffness = barStiffness;
        }

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (left == null || right == null || body == null || body.isKinematic || stiffness <= 0f)
            {
                return;
            }

            WheelHit hit;
            float travelLeft = 1f;
            float travelRight = 1f;

            bool groundedLeft = left.GetGroundHit(out hit);
            if (groundedLeft)
            {
                travelLeft = (-left.transform.InverseTransformPoint(hit.point).y - left.radius) / left.suspensionDistance;
            }

            bool groundedRight = right.GetGroundHit(out hit);
            if (groundedRight)
            {
                travelRight = (-right.transform.InverseTransformPoint(hit.point).y - right.radius) / right.suspensionDistance;
            }

            float force = (travelLeft - travelRight) * stiffness;

            if (groundedLeft)
            {
                body.AddForceAtPosition(left.transform.up * -force, left.transform.position);
            }

            if (groundedRight)
            {
                body.AddForceAtPosition(right.transform.up * force, right.transform.position);
            }
        }
    }
}
