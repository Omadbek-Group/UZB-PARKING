using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.AudioSystem;
using UzbCarParking.Core;
using UzbCarParking.VehiclePhysics;

namespace UzbCarParking.Vehicle
{
    /// <summary>
    /// Haqiqiy 3D model tayyor bo‘lguncha ishlatiladigan primitive (kub/silindr) mashina.
    /// Tuzilishi haqiqiy prefab bilan bir xil: Rigidbody + 4 ta WheelCollider + alohida g‘ildirak
    /// modellari + chiroqlar + davlat raqami. Bu HAQIQIY AVTOMOBIL MODELI EMAS.
    /// </summary>
    public static class PlaceholderVehicleFactory
    {
        private static readonly string[] WheelNames = { "FL", "FR", "RL", "RR" };

        public static VehicleBundle Create(VehicleConfig cfg, Vector3 position, Quaternion rotation, bool withPhysics)
        {
            GameObject rootObject = new GameObject("Vehicle_" + cfg.vehicleId);
            Transform root = rootObject.transform;
            root.SetPositionAndRotation(position, rotation);

            // Rigidbody WheelCollider'lardan oldin qo‘shilishi kerak.
            if (withPhysics)
            {
                Rigidbody body = rootObject.AddComponent<Rigidbody>();
                body.mass = cfg.mass;
            }

            float length = cfg.length;
            float width = cfg.width;
            float height = cfg.height;
            float radius = cfg.wheelRadius;
            float wheelWidth = cfg.wheelWidth;
            float halfTrack = cfg.trackWidth * 0.5f;
            float halfBase = cfg.wheelBase * 0.5f;

            Material paint = RuntimeMaterials.CreateInstance(MaterialKind.CarPaint, new Color(0.9f, 0.9f, 0.9f));
            Material glass = RuntimeMaterials.Get(MaterialKind.Glass);
            Material plastic = RuntimeMaterials.Get(MaterialKind.Plastic);
            Material tyre = RuntimeMaterials.Get(MaterialKind.Tyre);
            Material rim = RuntimeMaterials.Get(MaterialKind.Rim);
            Material lamp = RuntimeMaterials.Get(MaterialKind.Emissive);
            Material plateWhite = RuntimeMaterials.Get(MaterialKind.LineWhite);

            List<Renderer> paintRenderers = new List<Renderer>();
            List<Renderer> rimRenderers = new List<Renderer>();

            // ---------------------------------------------------------------- Kuzov
            Transform bodyRoot = PrimitiveKit.Child(root, "Body");
            const float sillY = 0.32f;
            const float fenderY = 0.71f;
            const float beltY = 0.9f;
            float coreWidth = Mathf.Max(0.6f, cfg.trackWidth - wheelWidth - 0.04f);

            paintRenderers.Add(Paint(PrimitiveKit.Visual(PrimitiveType.Cube, "LowerBody", bodyRoot,
                new Vector3(0f, (sillY + beltY) * 0.5f, 0f), new Vector3(coreWidth, beltY - sillY, length), paint)));
            paintRenderers.Add(Paint(PrimitiveKit.Visual(PrimitiveType.Cube, "UpperSide", bodyRoot,
                new Vector3(0f, (fenderY + beltY) * 0.5f, 0f), new Vector3(width, beltY - fenderY, length), paint)));
            paintRenderers.Add(Paint(PrimitiveKit.Visual(PrimitiveType.Cube, "Sill", bodyRoot,
                new Vector3(0f, 0.44f, 0f),
                new Vector3(width * 0.97f, 0.2f, Mathf.Max(0.3f, cfg.wheelBase - 2f * radius - 0.2f)), paint)));

            PrimitiveKit.Visual(PrimitiveType.Cube, "FrontBumper", bodyRoot,
                new Vector3(0f, 0.45f, length * 0.5f - 0.05f), new Vector3(width * 0.98f, 0.22f, 0.16f), plastic);
            PrimitiveKit.Visual(PrimitiveType.Cube, "RearBumper", bodyRoot,
                new Vector3(0f, 0.45f, -length * 0.5f + 0.05f), new Vector3(width * 0.98f, 0.22f, 0.16f), plastic);

            float cabinLength = length * 0.55f;
            float cabinZ = -length * 0.06f;
            float cabinHeight = height - beltY;
            float cabinMidY = (beltY + height) * 0.5f;

            PrimitiveKit.Visual(PrimitiveType.Cube, "Cabin", bodyRoot,
                new Vector3(0f, cabinMidY, cabinZ), new Vector3(width * 0.9f, cabinHeight, cabinLength), glass);
            paintRenderers.Add(Paint(PrimitiveKit.Visual(PrimitiveType.Cube, "Roof", bodyRoot,
                new Vector3(0f, height - 0.02f, cabinZ), new Vector3(width * 0.92f, 0.06f, cabinLength * 0.94f), paint)));

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    paintRenderers.Add(Paint(PrimitiveKit.Visual(PrimitiveType.Cube, "Pillar", bodyRoot,
                        new Vector3(sx * width * 0.45f, cabinMidY, cabinZ + sz * cabinLength * 0.5f),
                        new Vector3(0.07f, cabinHeight, 0.07f), paint)));
                }
            }

            // ---------------------------------------------------------------- Salon
            PrimitiveKit.Visual(PrimitiveType.Cube, "Dashboard", bodyRoot,
                new Vector3(0f, beltY + 0.05f, cabinZ + cabinLength * 0.5f - 0.2f),
                new Vector3(width * 0.85f, 0.18f, 0.3f), plastic);

            Transform steeringWheel = PrimitiveKit.Child(bodyRoot, "SteeringWheel");
            steeringWheel.localPosition = new Vector3(cfg.driverEyeLocalPosition.x, beltY + 0.12f,
                cabinZ + cabinLength * 0.5f - 0.42f);
            steeringWheel.localRotation = Quaternion.Euler(25f, 0f, 0f);
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Ring", steeringWheel, Vector3.zero,
                new Vector3(0.38f, 0.012f, 0.38f), plastic, Quaternion.Euler(90f, 0f, 0f));
            PrimitiveKit.Visual(PrimitiveType.Cube, "Spoke", steeringWheel, new Vector3(0f, 0f, -0.014f),
                new Vector3(0.36f, 0.045f, 0.012f), rim);

            // ---------------------------------------------------------------- Chiroqlar
            Transform lampsRoot = PrimitiveKit.Child(root, "Lights");
            float front = length * 0.5f + 0.005f;
            float rear = -length * 0.5f - 0.005f;

            Renderer headL = Lamp(lampsRoot, "Head_L", new Vector3(-width * 0.3f, 0.79f, front), new Vector3(0.28f, 0.12f, 0.04f), lamp);
            Renderer headR = Lamp(lampsRoot, "Head_R", new Vector3(width * 0.3f, 0.79f, front), new Vector3(0.28f, 0.12f, 0.04f), lamp);
            Renderer tailL = Lamp(lampsRoot, "Tail_L", new Vector3(-width * 0.32f, 0.8f, rear), new Vector3(0.26f, 0.12f, 0.04f), lamp);
            Renderer tailR = Lamp(lampsRoot, "Tail_R", new Vector3(width * 0.32f, 0.8f, rear), new Vector3(0.26f, 0.12f, 0.04f), lamp);
            Renderer revL = Lamp(lampsRoot, "Reverse_L", new Vector3(-width * 0.18f, 0.63f, rear), new Vector3(0.1f, 0.06f, 0.04f), lamp);
            Renderer revR = Lamp(lampsRoot, "Reverse_R", new Vector3(width * 0.18f, 0.63f, rear), new Vector3(0.1f, 0.06f, 0.04f), lamp);
            Renderer indFl = Lamp(lampsRoot, "Indicator_FL", new Vector3(-width * 0.45f, 0.79f, front), new Vector3(0.09f, 0.08f, 0.04f), lamp);
            Renderer indFr = Lamp(lampsRoot, "Indicator_FR", new Vector3(width * 0.45f, 0.79f, front), new Vector3(0.09f, 0.08f, 0.04f), lamp);
            Renderer indRl = Lamp(lampsRoot, "Indicator_RL", new Vector3(-width * 0.45f, 0.8f, rear), new Vector3(0.09f, 0.08f, 0.04f), lamp);
            Renderer indRr = Lamp(lampsRoot, "Indicator_RR", new Vector3(width * 0.45f, 0.8f, rear), new Vector3(0.09f, 0.08f, 0.04f), lamp);

            Light beamL = Beam(lampsRoot, "Beam_L", new Vector3(-width * 0.3f, 0.79f, length * 0.5f + 0.1f));
            Light beamR = Beam(lampsRoot, "Beam_R", new Vector3(width * 0.3f, 0.79f, length * 0.5f + 0.1f));

            // ---------------------------------------------------------------- Davlat raqami
            Transform plates = PrimitiveKit.Child(root, "Plates");
            float plateRearZ = -length * 0.5f - 0.035f;
            float plateFrontZ = length * 0.5f + 0.035f;
            PrimitiveKit.Visual(PrimitiveType.Cube, "PlateRear", plates, new Vector3(0f, 0.47f, plateRearZ),
                new Vector3(0.52f, 0.115f, 0.01f), plateWhite);
            PrimitiveKit.Visual(PrimitiveType.Cube, "PlateFront", plates, new Vector3(0f, 0.47f, plateFrontZ),
                new Vector3(0.52f, 0.115f, 0.01f), plateWhite);
            TextMesh plateRearText = PrimitiveKit.Label("01 A 777 AA", plates,
                new Vector3(0f, 0.47f, plateRearZ - 0.007f), Quaternion.identity, 0.011f, Color.black);
            TextMesh plateFrontText = PrimitiveKit.Label("01 A 777 AA", plates,
                new Vector3(0f, 0.47f, plateFrontZ + 0.007f), Quaternion.Euler(0f, 180f, 0f), 0.011f, Color.black);

            // ---------------------------------------------------------------- G‘ildiraklar
            Transform wheelVisualRoot = PrimitiveKit.Child(root, "WheelVisuals");
            Transform wheelColliderRoot = withPhysics ? PrimitiveKit.Child(root, "WheelColliders") : null;
            VehicleController.WheelSetup[] setups = new VehicleController.WheelSetup[4];

            for (int i = 0; i < 4; i++)
            {
                bool isFront = i < 2;
                float x = (i % 2 == 0) ? -halfTrack : halfTrack;
                float z = isFront ? halfBase : -halfBase;

                Transform visual = PrimitiveKit.Child(wheelVisualRoot, "Wheel_" + WheelNames[i]);
                visual.localPosition = new Vector3(x, radius, z);
                Quaternion axle = Quaternion.Euler(0f, 0f, 90f);

                PrimitiveKit.Visual(PrimitiveType.Cylinder, "Tyre", visual, Vector3.zero,
                    new Vector3(radius * 2f, wheelWidth * 0.5f, radius * 2f), tyre, axle);
                rimRenderers.Add(PrimitiveKit.Visual(PrimitiveType.Cylinder, "Rim", visual, Vector3.zero,
                    new Vector3(radius * 1.25f, wheelWidth * 0.5f + 0.004f, radius * 1.25f), rim, axle)
                    .GetComponent<Renderer>());
                PrimitiveKit.Visual(PrimitiveType.Cube, "Spoke_A", visual, Vector3.zero,
                    new Vector3(wheelWidth + 0.014f, radius * 1.15f, 0.06f), plastic);
                PrimitiveKit.Visual(PrimitiveType.Cube, "Spoke_B", visual, Vector3.zero,
                    new Vector3(wheelWidth + 0.014f, 0.06f, radius * 1.15f), plastic);

                if (!withPhysics)
                {
                    continue;
                }

                Transform colliderTransform = PrimitiveKit.Child(wheelColliderRoot, "WC_" + WheelNames[i]);
                colliderTransform.localPosition = new Vector3(x,
                    radius + cfg.suspensionDistance * (1f - cfg.suspensionTargetPosition), z);
                WheelCollider wheelCollider = colliderTransform.gameObject.AddComponent<WheelCollider>();
                wheelCollider.radius = radius;
                wheelCollider.suspensionDistance = cfg.suspensionDistance;

                setups[i] = new VehicleController.WheelSetup
                {
                    collider = wheelCollider,
                    visual = visual,
                    isFront = isFront,
                    steer = isFront
                };
            }

            // ---------------------------------------------------------------- Komponentlar
            VehicleBundle bundle = new VehicleBundle();
            bundle.Root = rootObject;

            if (withPhysics)
            {
                BoxCollider lowerCollider = rootObject.AddComponent<BoxCollider>();
                lowerCollider.center = new Vector3(0f, (sillY + beltY) * 0.5f + 0.02f, 0f);
                lowerCollider.size = new Vector3(width * 0.98f, beltY - sillY - 0.04f, length);

                BoxCollider cabinCollider = rootObject.AddComponent<BoxCollider>();
                cabinCollider.center = new Vector3(0f, cabinMidY, cabinZ);
                cabinCollider.size = new Vector3(width * 0.9f, cabinHeight, cabinLength);

                bundle.Controller = rootObject.AddComponent<VehicleController>();
                bundle.Controller.Initialize(cfg, setups, steeringWheel);

                AntiRollBar frontBar = rootObject.AddComponent<AntiRollBar>();
                frontBar.Setup(setups[0].collider, setups[1].collider, cfg.antiRollStiffness);
                AntiRollBar rearBar = rootObject.AddComponent<AntiRollBar>();
                rearBar.Setup(setups[2].collider, setups[3].collider, cfg.antiRollStiffness);
            }

            VehicleLights lights = rootObject.AddComponent<VehicleLights>();
            lights.headlights.renderers = new[] { headL, headR };
            lights.headlights.lights = new[] { beamL, beamR };
            lights.headlights.onColor = new Color(1f, 0.97f, 0.88f);
            lights.headlights.offColor = new Color(0.55f, 0.56f, 0.58f);
            lights.headlights.emissionIntensity = 3f;

            lights.brakeLights.renderers = new[] { tailL, tailR };
            lights.brakeLights.onColor = new Color(1f, 0.05f, 0.03f);
            lights.brakeLights.offColor = new Color(0.3f, 0.03f, 0.03f);
            lights.brakeLights.emissionIntensity = 3f;

            lights.reverseLights.renderers = new[] { revL, revR };
            lights.reverseLights.onColor = Color.white;
            lights.reverseLights.offColor = new Color(0.45f, 0.45f, 0.45f);

            lights.leftIndicators.renderers = new[] { indFl, indRl };
            lights.leftIndicators.onColor = new Color(1f, 0.6f, 0.05f);
            lights.leftIndicators.offColor = new Color(0.4f, 0.26f, 0.05f);
            lights.leftIndicators.emissionIntensity = 3f;

            lights.rightIndicators.renderers = new[] { indFr, indRr };
            lights.rightIndicators.onColor = new Color(1f, 0.6f, 0.05f);
            lights.rightIndicators.offColor = new Color(0.4f, 0.26f, 0.05f);
            lights.rightIndicators.emissionIntensity = 3f;

            lights.Init(bundle.Controller);
            bundle.Lights = lights;

            VehicleCustomization customization = rootObject.AddComponent<VehicleCustomization>();
            customization.paintRenderers = paintRenderers.ToArray();
            customization.rimRenderers = rimRenderers.ToArray();
            customization.plateTexts = new[] { plateRearText, plateFrontText };
            bundle.Customization = customization;

            if (withPhysics)
            {
                bundle.Damage = rootObject.AddComponent<VehicleDamage>();
                bundle.Damage.Init(bundle.Controller);

                bundle.Audio = rootObject.AddComponent<VehicleAudio>();
                bundle.Audio.Init(bundle.Controller, lights);
            }

            return bundle;
        }

        private static Renderer Paint(GameObject go)
        {
            return go.GetComponent<Renderer>();
        }

        private static Renderer Lamp(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            return PrimitiveKit.Visual(PrimitiveType.Cube, name, parent, position, size, material)
                .GetComponent<Renderer>();
        }

        private static Light Beam(Transform parent, string name, Vector3 position)
        {
            Transform t = PrimitiveKit.Child(parent, name);
            t.localPosition = position;
            t.localRotation = Quaternion.Euler(6f, 0f, 0f);

            Light beam = t.gameObject.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.spotAngle = 70f;
            beam.range = 25f;
            beam.intensity = 2.5f;
            beam.color = new Color(1f, 0.96f, 0.86f);
            beam.shadows = LightShadows.None;
            beam.enabled = false;
            return beam;
        }
    }
}
