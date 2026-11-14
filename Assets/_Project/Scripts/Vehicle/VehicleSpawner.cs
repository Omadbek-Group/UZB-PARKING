using UnityEngine;
using UzbCarParking.AudioSystem;

namespace UzbCarParking.Vehicle
{
    /// <summary>Yaratilgan mashinaning asosiy komponentlari.</summary>
    public sealed class VehicleBundle
    {
        public GameObject Root;
        public VehicleController Controller;
        public VehicleLights Lights;
        public VehicleCustomization Customization;
        public VehicleDamage Damage;
        public VehicleAudio Audio;
    }

    /// <summary>
    /// Mashinani sahnaga chiqaradi. VehicleConfig.vehiclePrefab biriktirilgan bo‘lsa — prefab,
    /// aks holda primitive placeholder ishlatiladi. Shu tufayli yangi 3D modellar
    /// kodni o‘zgartirmasdan qo‘shiladi.
    /// </summary>
    public static class VehicleSpawner
    {
        /// <param name="withPhysics">false — garaj ko‘rgazmasi uchun (fizikasiz).</param>
        public static VehicleBundle Spawn(VehicleConfig config, Vector3 position, Quaternion rotation, bool withPhysics)
        {
            if (config == null)
            {
                Debug.LogError("[UZB] VehicleSpawner: VehicleConfig yo‘q.");
                return null;
            }

            if (config.vehiclePrefab == null)
            {
                return PlaceholderVehicleFactory.Create(config, position, rotation, withPhysics);
            }

            return FromPrefab(config, position, rotation, withPhysics);
        }

        private static VehicleBundle FromPrefab(VehicleConfig config, Vector3 position, Quaternion rotation, bool withPhysics)
        {
            GameObject root = Object.Instantiate(config.vehiclePrefab, position, rotation);
            root.name = "Vehicle_" + config.vehicleId;

            VehicleBundle bundle = new VehicleBundle
            {
                Root = root,
                Controller = root.GetComponent<VehicleController>(),
                Lights = root.GetComponent<VehicleLights>(),
                Customization = root.GetComponent<VehicleCustomization>(),
                Damage = root.GetComponent<VehicleDamage>(),
                Audio = root.GetComponent<VehicleAudio>()
            };

            if (!withPhysics)
            {
                if (bundle.Controller != null) bundle.Controller.enabled = false;
                if (bundle.Audio != null) bundle.Audio.enabled = false;

                Rigidbody body = root.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;

                WheelCollider[] wheelColliders = root.GetComponentsInChildren<WheelCollider>();
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    wheelColliders[i].enabled = false;
                }

                if (bundle.Lights != null) bundle.Lights.Init(null);
                return bundle;
            }

            if (bundle.Controller == null)
            {
                Debug.LogError("[UZB] '" + config.vehiclePrefab.name + "' prefabida VehicleController yo‘q. " +
                               "Docs/VEHICLE_PREFAB_GUIDE.md ga qarang.", root);
                return bundle;
            }

            if (bundle.Controller.Config == null)
            {
                Debug.LogError("[UZB] Prefabdagi VehicleController'ga VehicleConfig biriktirilmagan.", root);
            }

            if (bundle.Lights != null)
            {
                bundle.Lights.Init(bundle.Controller);
            }

            if (bundle.Damage == null)
            {
                bundle.Damage = root.AddComponent<VehicleDamage>();
            }

            bundle.Damage.Init(bundle.Controller);

            if (bundle.Audio == null)
            {
                bundle.Audio = root.AddComponent<VehicleAudio>();
            }

            bundle.Audio.Init(bundle.Controller, bundle.Lights);
            return bundle;
        }
    }
}
