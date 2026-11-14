using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Avtodrom konusi. Mashina tegsa, surilsa yoki ag‘darilsa bir marta "urildi" deb qayd etiladi.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrafficCone : MonoBehaviour
    {
        public static event Action<TrafficCone> ConeHit;

        public float displacementThreshold = 0.15f;
        public float tiltThresholdDegrees = 25f;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Rigidbody body;
        private float settleTimer = 1f;

        public bool WasHit { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Update()
        {
            if (WasHit)
            {
                return;
            }

            // Sahna yuklangach konus joyiga o‘tirib olishi uchun qisqa kutish.
            if (settleTimer > 0f)
            {
                settleTimer -= Time.deltaTime;
                if (settleTimer <= 0f)
                {
                    startPosition = transform.position;
                    startRotation = transform.rotation;
                }

                return;
            }

            Vector3 offset = transform.position - startPosition;
            offset.y = 0f;
            bool displaced = offset.sqrMagnitude > displacementThreshold * displacementThreshold;
            bool tilted = Vector3.Angle(transform.up, Vector3.up) > tiltThresholdDegrees;
            if (displaced || tilted)
            {
                RegisterHit();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (WasHit || collision.collider == null)
            {
                return;
            }

            Rigidbody other = collision.collider.attachedRigidbody;
            if (other != null && other.GetComponent<VehicleController>() != null)
            {
                RegisterHit();
            }
        }

        private void RegisterHit()
        {
            WasHit = true;
            if (ConeHit != null)
            {
                ConeHit.Invoke(this);
            }
        }

        public void ResetCone()
        {
            WasHit = false;
            settleTimer = 0.5f;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
