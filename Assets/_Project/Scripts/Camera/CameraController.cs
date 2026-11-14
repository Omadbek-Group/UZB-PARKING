using System;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.CameraSystem
{
    public enum CameraMode
    {
        Follow = 0,
        Interior = 1,
        TopDown = 2
    }

    /// <summary>
    /// Uchta kamera rejimi: mashina ortidan kuzatuv, salon (haydovchi ko‘zi) va yuqoridan parkovka.
    /// Kapot va erkin aylanuvchi kamera keyingi bosqichda shu enum'ga qo‘shiladi.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Kuzatuv kamerasi")]
        public float followDistance = 6f;
        public float followHeight = 2.3f;
        public float followLookHeight = 1f;
        public float followSmoothTime = 0.12f;
        public float followYawSmoothTime = 0.25f;
        public float followFieldOfView = 60f;

        [Header("Salon kamerasi")]
        public float interiorFieldOfView = 72f;
        public float interiorPitch = 6f;

        [Header("Yuqoridan kamera")]
        public float topDownHeight = 13f;
        public float topDownFieldOfView = 50f;
        [Tooltip("true — kamera mashina bilan birga buriladi (mashina har doim ekranda yuqoriga qaragan).")]
        public bool topDownRotateWithVehicle = true;

        public event Action<CameraMode> ModeChanged;

        private Camera cam;
        private Transform target;
        private VehicleConfig config;
        private Vector3 followVelocity;
        private float followYaw;
        private float followYawVelocity;
        private bool snapNextFrame = true;

        public CameraMode Mode { get; private set; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform vehicleTransform, VehicleConfig vehicleConfig)
        {
            target = vehicleTransform;
            config = vehicleConfig;
            if (config != null)
            {
                followDistance = config.followDistance;
                followHeight = config.followHeight;
            }

            if (target != null)
            {
                followYaw = target.eulerAngles.y;
            }

            snapNextFrame = true;
        }

        public void SetMode(CameraMode mode)
        {
            Mode = mode;
            snapNextFrame = true;
            if (ModeChanged != null)
            {
                ModeChanged.Invoke(mode);
            }
        }

        public void NextMode()
        {
            SetMode((CameraMode)(((int)Mode + 1) % 3));
        }

        public static string ModeName(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Interior: return "Salon";
                case CameraMode.TopDown: return "Yuqoridan";
                default: return "Ortidan";
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            switch (Mode)
            {
                case CameraMode.Interior:
                    UpdateInterior();
                    break;
                case CameraMode.TopDown:
                    UpdateTopDown();
                    break;
                default:
                    UpdateFollow();
                    break;
            }

            snapNextFrame = false;
        }

        private void UpdateFollow()
        {
            if (cam != null)
            {
                cam.fieldOfView = followFieldOfView;
                cam.nearClipPlane = 0.3f;
            }

            float targetYaw = target.eulerAngles.y;
            if (snapNextFrame)
            {
                followYaw = targetYaw;
                followYawVelocity = 0f;
            }
            else
            {
                followYaw = Mathf.SmoothDampAngle(followYaw, targetYaw, ref followYawVelocity, followYawSmoothTime);
            }

            Vector3 offset = Quaternion.Euler(0f, followYaw, 0f) * new Vector3(0f, followHeight, -followDistance);
            Vector3 desired = target.position + offset;

            if (snapNextFrame)
            {
                transform.position = desired;
                followVelocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, followSmoothTime);
            }

            Vector3 lookPoint = target.position + Vector3.up * followLookHeight;
            Vector3 direction = lookPoint - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void UpdateInterior()
        {
            if (cam != null)
            {
                cam.fieldOfView = interiorFieldOfView;
                cam.nearClipPlane = 0.05f;
            }

            Vector3 eye = config != null ? config.driverEyeLocalPosition : new Vector3(-0.35f, 1.15f, 0f);
            transform.SetPositionAndRotation(target.TransformPoint(eye),
                target.rotation * Quaternion.Euler(interiorPitch, 0f, 0f));
        }

        private void UpdateTopDown()
        {
            if (cam != null)
            {
                cam.fieldOfView = topDownFieldOfView;
                cam.nearClipPlane = 0.3f;
            }

            float yaw = topDownRotateWithVehicle ? target.eulerAngles.y : 0f;
            transform.SetPositionAndRotation(target.position + Vector3.up * topDownHeight,
                Quaternion.Euler(90f, yaw, 0f));
        }
    }
}
