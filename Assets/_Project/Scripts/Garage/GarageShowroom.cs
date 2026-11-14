using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.Persistence;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Garage
{
    /// <summary>
    /// Oddiy 3D garaj: aylanadigan podium ustidagi mashina. Bosh menyu foni va tuning ko‘rinishi sifatida ishlatiladi.
    /// Geometriya — primitive placeholder; haqiqiy garaj modeli keyingi bosqichda.
    /// </summary>
    public class GarageShowroom : MonoBehaviour
    {
        public float rotationSpeed = 12f;

        private Transform turntable;

        public VehicleBundle Bundle { get; private set; }

        public VehicleCustomization Customization
        {
            get { return Bundle != null ? Bundle.Customization : null; }
        }

        public static GarageShowroom Build(VehicleConfig config, VehicleCustomizationData customization)
        {
            GameObject root = new GameObject("Garage");
            GarageShowroom showroom = root.AddComponent<GarageShowroom>();
            showroom.BuildInternal(config, customization);
            return showroom;
        }

        private void BuildInternal(VehicleConfig config, VehicleCustomizationData customization)
        {
            Transform root = transform;
            Material floor = RuntimeMaterials.Get(MaterialKind.GarageFloor);
            Material wall = RuntimeMaterials.Get(MaterialKind.GarageWall);
            Material plastic = RuntimeMaterials.Get(MaterialKind.Plastic);

            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Floor", root, new Vector3(0f, -0.05f, 0f), new Vector3(34f, 0.05f, 34f), floor);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Wall_Back", root, new Vector3(0f, 4f, 10f), new Vector3(40f, 8f, 0.4f), wall);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Wall_Left", root, new Vector3(-13f, 4f, 0f), new Vector3(0.4f, 8f, 26f), wall);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Wall_Right", root, new Vector3(13f, 4f, 0f), new Vector3(0.4f, 8f, 26f), wall);

            // Yorug‘ chiziqlar (dekor).
            Material glow = RuntimeMaterials.CreateInstance(MaterialKind.Emissive, new Color(0.16f, 0.83f, 1f));
            RuntimeMaterials.SetEmission(glow, new Color(0.16f, 0.83f, 1f) * 2.2f);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Strip_Back", root, new Vector3(0f, 2.6f, 9.75f), new Vector3(18f, 0.08f, 0.05f), glow);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Strip_Back2", root, new Vector3(0f, 5.2f, 9.75f), new Vector3(26f, 0.08f, 0.05f), glow);
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Ring", root, new Vector3(0f, 0.006f, 0f), new Vector3(7.5f, 0.005f, 7.5f), glow);

            turntable = PrimitiveKit.Child(root, "Turntable");
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Disc", turntable, new Vector3(0f, 0.02f, 0f), new Vector3(7.2f, 0.02f, 7.2f), plastic);

            CreateSpot(root, "KeyLight", new Vector3(-4f, 6f, -5f), 6f, new Color(1f, 0.97f, 0.92f));
            CreateSpot(root, "RimLight", new Vector3(5f, 5f, 5f), 4f, new Color(0.6f, 0.8f, 1f));
            CreateSpot(root, "FillLight", new Vector3(4f, 4f, -6f), 2.5f, Color.white);

            Bundle = VehicleSpawner.Spawn(config, new Vector3(0f, 0.04f, 0f), Quaternion.Euler(0f, 215f, 0f), false);
            if (Bundle != null && Bundle.Root != null)
            {
                Bundle.Root.transform.SetParent(turntable, true);
                if (Bundle.Customization != null)
                {
                    Bundle.Customization.Apply(customization);
                }

                if (Bundle.Lights != null)
                {
                    Bundle.Lights.SetHeadlights(HeadlightMode.LowBeam);
                }
            }
        }

        private static void CreateSpot(Transform parent, string name, Vector3 position, float intensity, Color color)
        {
            Transform t = PrimitiveKit.Child(parent, name);
            t.position = position;
            t.rotation = Quaternion.LookRotation(new Vector3(0f, 0.6f, 0f) - position);

            Light spot = t.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 65f;
            spot.range = 22f;
            spot.intensity = intensity;
            spot.color = color;
            spot.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (turntable != null)
            {
                turntable.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
            }
        }
    }
}
