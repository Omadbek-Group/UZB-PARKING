using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>Keyingi mashq joyini ko‘rsatuvchi yorug‘ ustun.</summary>
    public class ExerciseBeacon : MonoBehaviour
    {
        public float bobHeight = 0.35f;
        public float bobSpeed = 2f;
        public float spinSpeed = 90f;

        private Transform arrow;
        private float baseArrowY;

        public void Init(Transform arrowTransform)
        {
            arrow = arrowTransform;
            baseArrowY = arrow != null ? arrow.localPosition.y : 0f;
        }

        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = new Vector3(worldPosition.x, 0f, worldPosition.z);
        }

        private void Update()
        {
            if (arrow == null)
            {
                return;
            }

            Vector3 local = arrow.localPosition;
            local.y = baseArrowY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            arrow.localPosition = local;
            arrow.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
