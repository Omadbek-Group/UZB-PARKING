using UnityEngine;

namespace UzbCarParking.Vehicle
{
    public enum DriveType
    {
        FrontWheelDrive = 0,
        RearWheelDrive = 1,
        AllWheelDrive = 2
    }

    /// <summary>
    /// Bitta avtomobilning barcha fizik va o‘yin parametrlari.
    /// Har bir model uchun alohida asset: Create ▸ UZB Car Parking ▸ Vehicle Config.
    /// Standart qiymatlar kichik shahar xetchbekiga (≈1.0 l, old yuritma) mo‘ljallangan placeholder.
    /// </summary>
    [CreateAssetMenu(menuName = "UZB Car Parking/Vehicle Config", fileName = "VC_NewVehicle")]
    public class VehicleConfig : ScriptableObject
    {
        [Header("Identifikatsiya")]
        public string vehicleId = "placeholder_hatchback";
        public string displayName = "Sinov xetchbeki (placeholder)";

        [Tooltip("Biriktirilsa shu prefab ishlatiladi. Bo‘sh bo‘lsa primitive placeholder mashina yaratiladi.")]
        public GameObject vehiclePrefab;

        [Header("O‘lchamlar (metr)")]
        public float length = 3.6f;
        public float width = 1.6f;
        public float height = 1.5f;
        public float wheelBase = 2.38f;
        public float trackWidth = 1.4f;

        [Tooltip("Mashina pivoti markazda/yerda bo‘lmasa, gabarit markazini shu yerda to‘g‘rilang (lokal).")]
        public Vector3 footprintCenterOffset = Vector3.zero;

        [Header("Massa va inersiya")]
        public float mass = 1000f;
        public Vector3 centerOfMassOffset = new Vector3(0f, 0.38f, 0.1f);
        public float linearDamping = 0.02f;
        public float angularDamping = 0.5f;
        [Tooltip("Havo qarshiligi koeffitsiyenti: F = k · v²")]
        public float aerodynamicDrag = 0.4f;
        [Tooltip("Bosuvchi kuch koeffitsiyenti: F = k · v²")]
        public float downforce = 0.3f;

        [Header("Dvigatel")]
        public float idleRpm = 800f;
        public float maxRpm = 6200f;
        [Tooltip("X — aylanish (ayl/min), Y — burovchi moment (N·m)")]
        public AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(800f, 70f),
            new Keyframe(2000f, 85f),
            new Keyframe(3500f, 92f),
            new Keyframe(4800f, 88f),
            new Keyframe(6200f, 68f));
        [Tooltip("Dvigatel aylanishlarining javob vaqti (soniya). Kichik — tezroq.")]
        public float engineResponseTime = 0.12f;
        [Tooltip("Joyidan qo‘zg‘alishda (ilashma/gidrotransformator sirpanishi) qo‘shiladigan aylanish.")]
        public float launchRpmBoost = 1600f;
        [Tooltip("Gaz qo‘yib yuborilganda dvigatel bilan tormozlash (g‘ildirakdagi jami moment, N·m).")]
        public float engineBrakingTorque = 120f;

        [Header("Uzatmalar qutisi")]
        public DriveType driveType = DriveType.FrontWheelDrive;
        public float[] forwardGearRatios = { 3.73f, 2.05f, 1.32f, 0.97f, 0.76f };
        public float reverseGearRatio = 3.5f;
        public float finalDriveRatio = 4.2f;
        [Range(0.5f, 1f)] public float drivetrainEfficiency = 0.85f;
        public float autoShiftUpRpm = 4800f;
        public float autoShiftDownRpm = 1800f;
        public float shiftDuration = 0.25f;
        [Tooltip("Avtomat qutida D/R holatida gazsiz 'sudralish' momenti (N·m, jami).")]
        public float creepTorque = 180f;
        public float creepMaxSpeed = 2f;
        public bool requireBrakeToLeavePark = true;

        [Header("Tormoz")]
        [Tooltip("Bitta g‘ildirakka o‘rtacha maksimal tormoz momenti (N·m).")]
        public float maxBrakeTorque = 1500f;
        [Range(0.3f, 0.9f)] public float frontBrakeBias = 0.65f;
        public float handbrakeTorque = 2500f;
        public float parkLockTorque = 6000f;

        [Header("Rul")]
        public float maxSteerAngle = 35f;
        public float steerAngleAtHighSpeed = 12f;
        public float highSpeedKmh = 100f;
        [Tooltip("Old g‘ildiraklarning burilish tezligi (gradus/soniya).")]
        public float steerSpeed = 110f;
        [Tooltip("Rul chambaragining bir tomonga to‘liq aylanishi (gradus).")]
        public float steeringWheelMaxDegrees = 450f;

        [Header("G‘ildirak va osma")]
        public float wheelRadius = 0.29f;
        public float wheelWidth = 0.18f;
        public float wheelMass = 15f;
        public float wheelDampingRate = 0.25f;
        public float forceAppPointDistance = 0.1f;
        public float suspensionDistance = 0.18f;
        public float springStrength = 30000f;
        public float damperStrength = 3000f;
        [Range(0f, 1f)] public float suspensionTargetPosition = 0.5f;
        public float antiRollStiffness = 5000f;

        [Header("Shina ishqalanishi — bo‘ylama")]
        public float forwardExtremumSlip = 0.4f;
        public float forwardExtremumValue = 1f;
        public float forwardAsymptoteSlip = 0.8f;
        public float forwardAsymptoteValue = 0.5f;
        public float forwardStiffness = 1.5f;

        [Header("Shina ishqalanishi — ko‘ndalang")]
        public float sidewaysExtremumSlip = 0.25f;
        public float sidewaysExtremumValue = 1f;
        public float sidewaysAsymptoteSlip = 0.5f;
        public float sidewaysAsymptoteValue = 0.75f;
        public float sidewaysStiffness = 1.6f;

        [Header("Yordamchi tizimlar (simulyatsiya)")]
        public bool absEnabled = true;
        public float absSlipThreshold = 0.35f;
        [Range(0f, 1f)] public float absReleaseFactor = 0.3f;
        public bool tcsEnabled = true;
        public float tcsSlipThreshold = 0.3f;
        [Range(0f, 1f)] public float tcsTorqueFactor = 0.35f;

        [Header("Yoqilg‘i")]
        public float fuelCapacityLiters = 35f;
        public float startFuelLiters = 30f;
        public float idleFuelLitersPerHour = 0.8f;
        public float maxFuelLitersPerHour = 18f;

        [Header("Kamera")]
        [Tooltip("Haydovchi ko‘zi joyi (lokal). Chap rulli avtomobil uchun X manfiy.")]
        public Vector3 driverEyeLocalPosition = new Vector3(-0.35f, 1.15f, -0.05f);
        public float followDistance = 6f;
        public float followHeight = 2.3f;

        [Header("Shikastlanish")]
        [Tooltip("Shu tezlikdan (m/s) kuchli zarbalar shikast keltiradi.")]
        public float damageImpactThreshold = 3f;
        public float damagePerImpactUnit = 2f;

        [Header("Fizika aniqligi (WheelCollider substeps)")]
        public float substepSpeedThreshold = 5f;
        public int substepsBelowThreshold = 12;
        public int substepsAboveThreshold = 15;

        public int ForwardGearCount
        {
            get { return forwardGearRatios != null ? forwardGearRatios.Length : 0; }
        }

        private void OnValidate()
        {
            length = Mathf.Max(1f, length);
            width = Mathf.Max(0.5f, width);
            mass = Mathf.Max(100f, mass);
            wheelRadius = Mathf.Max(0.1f, wheelRadius);
            suspensionDistance = Mathf.Max(0.02f, suspensionDistance);
            idleRpm = Mathf.Max(300f, idleRpm);
            maxRpm = Mathf.Max(idleRpm + 500f, maxRpm);
            autoShiftUpRpm = Mathf.Clamp(autoShiftUpRpm, idleRpm + 500f, maxRpm);
            autoShiftDownRpm = Mathf.Clamp(autoShiftDownRpm, idleRpm, autoShiftUpRpm - 300f);
            fuelCapacityLiters = Mathf.Max(1f, fuelCapacityLiters);
            startFuelLiters = Mathf.Clamp(startFuelLiters, 0f, fuelCapacityLiters);
            substepsBelowThreshold = Mathf.Max(1, substepsBelowThreshold);
            substepsAboveThreshold = Mathf.Max(1, substepsAboveThreshold);

            if (forwardGearRatios == null || forwardGearRatios.Length == 0)
            {
                forwardGearRatios = new[] { 3.73f, 2.05f, 1.32f, 0.97f, 0.76f };
            }
        }
    }
}
