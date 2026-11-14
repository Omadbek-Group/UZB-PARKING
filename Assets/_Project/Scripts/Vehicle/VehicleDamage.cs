using System;
using UnityEngine;

namespace UzbCarParking.Vehicle
{
    /// <summary>
    /// To‘qnashuvlarni qayd etadi va shikastlanish foizini hisoblaydi.
    /// 1-bosqichda faqat son qiymati; kuzovning vizual deformatsiyasi keyingi bosqichda.
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleDamage : MonoBehaviour
    {
        /// <summary>Nisbiy tezlik (m/s) va urilgan kollayder.</summary>
        public event Action<float, Collider> Impact;

        private VehicleController vehicle;

        public float DamagePercent { get; private set; }

        public void Init(VehicleController vehicleController)
        {
            if (vehicle != null)
            {
                vehicle.Collided -= OnCollided;
            }

            vehicle = vehicleController;
            if (vehicle != null)
            {
                vehicle.Collided += OnCollided;
            }
        }

        private void OnDestroy()
        {
            if (vehicle != null)
            {
                vehicle.Collided -= OnCollided;
            }
        }

        public void Repair()
        {
            DamagePercent = 0f;
        }

        private void OnCollided(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (Impact != null)
            {
                Impact.Invoke(speed, collision.collider);
            }

            if (vehicle == null || vehicle.Config == null)
            {
                return;
            }

            float threshold = vehicle.Config.damageImpactThreshold;
            if (speed > threshold)
            {
                float added = (speed - threshold) * vehicle.Config.damagePerImpactUnit;
                DamagePercent = Mathf.Min(100f, DamagePercent + added);
            }
        }
    }
}
