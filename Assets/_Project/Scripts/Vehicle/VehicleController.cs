using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.VehiclePhysics;

namespace UzbCarParking.Vehicle
{
    /// <summary>
    /// WheelCollider asosidagi avtomobil boshqaruvi: dvigatel, uzatmalar qutisi (avtomat P/R/N/D va mexanik),
    /// tormoz, qo‘l tormozi, rul, ABS/TCS simulyatsiyasi, yoqilg‘i sarfi.
    /// Barcha parametrlar VehicleConfig (ScriptableObject) orqali Inspector'da sozlanadi.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        public class WheelSetup
        {
            public WheelCollider collider;
            [Tooltip("Aylanadigan g‘ildirak modeli (WheelCollider'ning bolasi bo‘lmasligi kerak).")]
            public Transform visual;
            public bool isFront;
            public bool steer;

            [NonSerialized] public float currentGrip = 1f;
        }

        [SerializeField] private VehicleConfig config;
        [SerializeField] private WheelSetup[] wheels = new WheelSetup[0];
        [Tooltip("Salondagi rul chambaragi. Lokal Z o‘qi rul ustuni bo‘ylab yo‘nalgan bo‘lishi kerak.")]
        [SerializeField] private Transform steeringWheelVisual;
        [SerializeField] private TransmissionType transmission = TransmissionType.Automatic;

        public event Action<bool> EngineStateChanged;
        public event Action<string> GearChanged;
        public event Action<Collision> Collided;

        private Rigidbody body;
        private VehicleInputState input;
        private float currentSteerAngle;
        private float shiftTimer;
        private Quaternion steeringWheelBaseRotation = Quaternion.identity;
        private bool initialized;
        private int drivenWheelCount;

        public VehicleConfig Config { get { return config; } }
        public Rigidbody Body { get { return body; } }
        public TransmissionType Transmission { get { return transmission; } }
        public AutoGearSelector Selector { get; private set; }

        /// <summary>Avtomat qutida D rejimidagi joriy uzatma (0 dan boshlanadi).</summary>
        public int AutoGearIndex { get; private set; }

        /// <summary>Mexanik quti: -1 — orqaga, 0 — neytral, 1..N — oldinga.</summary>
        public int ManualGear { get; private set; }

        public bool EngineRunning { get; private set; }
        public float EngineRpm { get; private set; }
        public float FuelLiters { get; private set; }
        public bool HandbrakeEngaged { get; private set; }
        public bool SeatbeltFastened { get; private set; }
        public bool HornActive { get; set; }

        /// <summary>Oldinga yo‘nalgan tezlik, m/s (orqaga harakatda manfiy).</summary>
        public float ForwardSpeed { get; private set; }

        public float ThrottleInput { get { return input.throttle; } }
        public float BrakeInput { get { return input.brake; } }
        public float SteerInput { get { return input.steer; } }

        public float SpeedKmh
        {
            get { return body != null ? body.linearVelocity.magnitude * GameConstants.MsToKmh : 0f; }
        }

        public float SteerAngleNormalized
        {
            get { return config != null && config.maxSteerAngle > 0.01f ? currentSteerAngle / config.maxSteerAngle : 0f; }
        }

        public float RpmNormalized
        {
            get { return config != null ? Mathf.Clamp01(EngineRpm / config.maxRpm) : 0f; }
        }

        public float FuelNormalized
        {
            get { return config != null ? Mathf.Clamp01(FuelLiters / config.fuelCapacityLiters) : 0f; }
        }

        public bool IsInPark
        {
            get { return transmission == TransmissionType.Automatic && Selector == AutoGearSelector.Park; }
        }

        public bool IsReverseSelected
        {
            get
            {
                return transmission == TransmissionType.Automatic
                    ? Selector == AutoGearSelector.Reverse
                    : ManualGear < 0;
            }
        }

        public bool IsForwardGearSelected
        {
            get
            {
                return transmission == TransmissionType.Automatic
                    ? Selector == AutoGearSelector.Drive
                    : ManualGear > 0;
            }
        }

        public string GearLabel
        {
            get
            {
                if (transmission == TransmissionType.Automatic)
                {
                    switch (Selector)
                    {
                        case AutoGearSelector.Park: return "P";
                        case AutoGearSelector.Reverse: return "R";
                        case AutoGearSelector.Neutral: return "N";
                        default: return "D" + (AutoGearIndex + 1);
                    }
                }

                if (ManualGear < 0) return "R";
                return ManualGear == 0 ? "N" : ManualGear.ToString();
            }
        }

        /// <summary>Oldingi bamper markazi (dunyo koordinatasida) — to‘xtash chizig‘ini tekshirish uchun.</summary>
        public Vector3 FrontPoint
        {
            get
            {
                Vector3 local = config.footprintCenterOffset + new Vector3(0f, 0.3f, config.length * 0.5f);
                return transform.TransformPoint(local);
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            // Prefab orqali kelgan mashina: maydonlar Inspector'da to‘ldirilgan bo‘ladi.
            if (config != null && wheels != null && wheels.Length > 0)
            {
                Initialize(config, wheels, steeringWheelVisual);
            }
        }

        /// <summary>Kod orqali yaratilgan mashinalar uchun (PlaceholderVehicleFactory).</summary>
        public void Initialize(VehicleConfig vehicleConfig, WheelSetup[] wheelSetups, Transform steeringWheel)
        {
            config = vehicleConfig;
            wheels = wheelSetups ?? new WheelSetup[0];
            steeringWheelVisual = steeringWheel;
            if (steeringWheelVisual != null)
            {
                steeringWheelBaseRotation = steeringWheelVisual.localRotation;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (config == null)
            {
                Debug.LogError("[UZB] VehicleController: VehicleConfig biriktirilmagan.", this);
                initialized = false;
                return;
            }

            ApplyConfig();
            ResetDriveState();
            initialized = true;
        }

        /// <summary>Config qiymatlarini Rigidbody va WheelCollider'larga qo‘llaydi.</summary>
        [ContextMenu("Config qiymatlarini qayta qo‘llash")]
        public void ApplyConfig()
        {
            if (config == null || body == null)
            {
                return;
            }

            body.mass = config.mass;
            body.linearDamping = config.linearDamping;
            body.angularDamping = config.angularDamping;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.centerOfMass = config.centerOfMassOffset;

            JointSpring spring = new JointSpring
            {
                spring = config.springStrength,
                damper = config.damperStrength,
                targetPosition = config.suspensionTargetPosition
            };

            WheelFrictionCurve forward = MakeCurve(config.forwardExtremumSlip, config.forwardExtremumValue,
                config.forwardAsymptoteSlip, config.forwardAsymptoteValue, config.forwardStiffness);
            WheelFrictionCurve sideways = MakeCurve(config.sidewaysExtremumSlip, config.sidewaysExtremumValue,
                config.sidewaysAsymptoteSlip, config.sidewaysAsymptoteValue, config.sidewaysStiffness);

            drivenWheelCount = 0;
            WheelCollider firstCollider = null;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null)
                {
                    continue;
                }

                WheelCollider wc = wheel.collider;
                wc.radius = config.wheelRadius;
                wc.mass = config.wheelMass;
                wc.wheelDampingRate = config.wheelDampingRate;
                wc.forceAppPointDistance = config.forceAppPointDistance;
                wc.suspensionDistance = config.suspensionDistance;
                wc.suspensionSpring = spring;
                wc.forwardFriction = forward;
                wc.sidewaysFriction = sideways;
                wheel.currentGrip = 1f;

                if (IsDriven(wheel))
                {
                    drivenWheelCount++;
                }

                if (firstCollider == null)
                {
                    firstCollider = wc;
                }
            }

            if (firstCollider != null)
            {
                firstCollider.ConfigureVehicleSubsteps(config.substepSpeedThreshold,
                    config.substepsBelowThreshold, config.substepsAboveThreshold);
            }
        }

        /// <summary>Boshlang‘ich holat: dvigatel o‘chiq, P/N, qo‘l tormozi tortilgan, kamar taqilmagan.</summary>
        public void ResetDriveState()
        {
            EngineRunning = false;
            EngineRpm = 0f;
            Selector = AutoGearSelector.Park;
            AutoGearIndex = 0;
            ManualGear = 0;
            HandbrakeEngaged = true;
            SeatbeltFastened = false;
            HornActive = false;
            FuelLiters = Mathf.Clamp(config.startFuelLiters, 0f, config.fuelCapacityLiters);
            input = default(VehicleInputState);
            currentSteerAngle = 0f;
            shiftTimer = 0f;
        }

        public void SetTransmission(TransmissionType type)
        {
            transmission = type;
            Selector = AutoGearSelector.Park;
            AutoGearIndex = 0;
            ManualGear = 0;
            RaiseGearChanged();
        }

        public void SetInput(VehicleInputState state)
        {
            input.throttle = Mathf.Clamp01(state.throttle);
            input.brake = Mathf.Clamp01(state.brake);
            input.steer = Mathf.Clamp(state.steer, -1f, 1f);
        }

        // ------------------------------------------------------------------ Dvigatel

        public bool TryStartEngine()
        {
            if (!initialized)
            {
                return false;
            }

            if (EngineRunning)
            {
                return true;
            }

            if (FuelLiters <= 0.01f)
            {
                GameEvents.Toast("Yoqilg‘i tugagan");
                return false;
            }

            if (transmission == TransmissionType.Automatic &&
                Selector != AutoGearSelector.Park && Selector != AutoGearSelector.Neutral)
            {
                GameEvents.Toast("Dvigatelni faqat P yoki N holatida yoqish mumkin");
                return false;
            }

            if (transmission == TransmissionType.Manual && ManualGear != 0 && input.brake < 0.1f)
            {
                GameEvents.Toast("Neytral uzatmaga o‘ting yoki tormozni bosing");
                return false;
            }

            EngineRunning = true;
            EngineRpm = config.idleRpm * 1.4f;
            if (EngineStateChanged != null)
            {
                EngineStateChanged.Invoke(true);
            }

            return true;
        }

        public void StopEngine()
        {
            if (!EngineRunning)
            {
                return;
            }

            EngineRunning = false;
            if (EngineStateChanged != null)
            {
                EngineStateChanged.Invoke(false);
            }
        }

        public void ToggleEngine()
        {
            if (EngineRunning)
            {
                StopEngine();
            }
            else
            {
                TryStartEngine();
            }
        }

        // ------------------------------------------------------------------ Qo‘l tormozi, kamar

        public void SetHandbrake(bool engaged)
        {
            HandbrakeEngaged = engaged;
        }

        public void ToggleHandbrake()
        {
            SetHandbrake(!HandbrakeEngaged);
        }

        public void SetSeatbelt(bool fastened)
        {
            SeatbeltFastened = fastened;
        }

        public void ToggleSeatbelt()
        {
            SeatbeltFastened = !SeatbeltFastened;
            GameEvents.Toast(SeatbeltFastened ? "Xavfsizlik kamari taqildi" : "Xavfsizlik kamari yechildi");
        }

        // ------------------------------------------------------------------ Uzatmalar

        public bool TrySelectAutoGear(AutoGearSelector target)
        {
            if (!initialized || transmission != TransmissionType.Automatic)
            {
                return false;
            }

            if (target == Selector)
            {
                return true;
            }

            if (target == AutoGearSelector.Reverse && ForwardSpeed > 1f)
            {
                GameEvents.Toast("R uchun avval to‘xtang");
                return false;
            }

            if (target == AutoGearSelector.Drive && ForwardSpeed < -1f)
            {
                GameEvents.Toast("D uchun avval to‘xtang");
                return false;
            }

            if (target == AutoGearSelector.Park && Mathf.Abs(ForwardSpeed) > 0.5f)
            {
                GameEvents.Toast("P uchun to‘liq to‘xtang");
                return false;
            }

            if (Selector == AutoGearSelector.Park && config.requireBrakeToLeavePark &&
                EngineRunning && input.brake < 0.1f)
            {
                GameEvents.Toast("P dan chiqish uchun tormozni bosib turing");
                return false;
            }

            Selector = target;
            AutoGearIndex = 0;
            shiftTimer = 0f;
            RaiseGearChanged();
            return true;
        }

        /// <summary>Avtomat: P→R→N→D yo‘nalishida bir qadam. Mexanik: keyingi uzatma.</summary>
        public void ShiftUp()
        {
            if (!initialized)
            {
                return;
            }

            if (transmission == TransmissionType.Automatic)
            {
                int next = Mathf.Min((int)AutoGearSelector.Drive, (int)Selector + 1);
                TrySelectAutoGear((AutoGearSelector)next);
                return;
            }

            if (ManualGear >= config.ForwardGearCount)
            {
                return;
            }

            ManualGear++;
            shiftTimer = config.shiftDuration;
            RaiseGearChanged();
        }

        /// <summary>Avtomat: D→N→R→P yo‘nalishida bir qadam. Mexanik: oldingi uzatma.</summary>
        public void ShiftDown()
        {
            if (!initialized)
            {
                return;
            }

            if (transmission == TransmissionType.Automatic)
            {
                int next = Mathf.Max((int)AutoGearSelector.Park, (int)Selector - 1);
                TrySelectAutoGear((AutoGearSelector)next);
                return;
            }

            if (ManualGear <= -1)
            {
                return;
            }

            if (ManualGear == 0 && ForwardSpeed > 1f)
            {
                GameEvents.Toast("Orqaga uzatma uchun avval to‘xtang");
                return;
            }

            ManualGear--;
            shiftTimer = config.shiftDuration;
            RaiseGearChanged();
        }

        private void RaiseGearChanged()
        {
            if (GearChanged != null)
            {
                GearChanged.Invoke(GearLabel);
            }
        }

        /// <summary>Joriy umumiy uzatish soni (uzatma × bosh uzatma). direction: 1 oldinga, -1 orqaga, 0 uzilgan.</summary>
        private float GetTotalRatio(out int direction)
        {
            direction = 0;
            if (transmission == TransmissionType.Automatic)
            {
                if (Selector == AutoGearSelector.Drive)
                {
                    direction = 1;
                    int index = Mathf.Clamp(AutoGearIndex, 0, config.ForwardGearCount - 1);
                    return config.forwardGearRatios[index] * config.finalDriveRatio;
                }

                if (Selector == AutoGearSelector.Reverse)
                {
                    direction = -1;
                    return config.reverseGearRatio * config.finalDriveRatio;
                }

                return 0f;
            }

            if (ManualGear > 0)
            {
                direction = 1;
                int index = Mathf.Clamp(ManualGear - 1, 0, config.ForwardGearCount - 1);
                return config.forwardGearRatios[index] * config.finalDriveRatio;
            }

            if (ManualGear < 0)
            {
                direction = -1;
                return config.reverseGearRatio * config.finalDriveRatio;
            }

            return 0f;
        }

        // ------------------------------------------------------------------ Fizika

        private void FixedUpdate()
        {
            if (!initialized)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            ForwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);

            UpdateSteering(dt);
            float engineBrake;
            float wheelTorque = UpdatePowertrain(dt, out engineBrake);
            ApplyWheelForces(wheelTorque, engineBrake);
            ApplyAerodynamics();
            UpdateFuel(dt);
        }

        private void UpdateSteering(float dt)
        {
            float speedFactor = Mathf.InverseLerp(0f, config.highSpeedKmh, SpeedKmh);
            float maxAngle = Mathf.Lerp(config.maxSteerAngle, config.steerAngleAtHighSpeed, speedFactor);
            float target = input.steer * maxAngle;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, target, config.steerSpeed * dt);

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel != null && wheel.steer && wheel.collider != null)
                {
                    wheel.collider.steerAngle = currentSteerAngle;
                }
            }
        }

        /// <returns>Yetaklovchi g‘ildiraklarga beriladigan jami moment (ishorali, N·m).</returns>
        private float UpdatePowertrain(float dt, out float engineBrakeTorque)
        {
            engineBrakeTorque = 0f;

            if (shiftTimer > 0f)
            {
                shiftTimer -= dt;
            }

            if (!EngineRunning)
            {
                EngineRpm = Mathf.MoveTowards(EngineRpm, 0f, 4000f * dt);
                return 0f;
            }

            int direction;
            float ratio = GetTotalRatio(out direction);
            bool inGear = direction != 0;
            bool shifting = shiftTimer > 0f;
            float throttle = input.throttle;
            float drivenRpm = Mathf.Abs(AverageDrivenWheelRpm());
            float wheelDrivenRpm = inGear ? drivenRpm * ratio : 0f;

            // Dvigatel aylanishlari: uzatmada — g‘ildiraklar bilan bog‘langan, lekin salt yurishdan pastga tushmaydi.
            float targetRpm;
            if (inGear)
            {
                float slipRpm = config.idleRpm + throttle * config.launchRpmBoost;
                targetRpm = Mathf.Max(wheelDrivenRpm, slipRpm);
            }
            else
            {
                targetRpm = config.idleRpm + throttle * (config.maxRpm - config.idleRpm);
            }

            targetRpm = Mathf.Min(targetRpm, config.maxRpm);
            float blend = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, config.engineResponseTime));
            EngineRpm = Mathf.Lerp(EngineRpm, targetRpm, blend);

            // Avtomat quti: D rejimida uzatmalarni o‘zi almashtiradi.
            if (transmission == TransmissionType.Automatic && Selector == AutoGearSelector.Drive && !shifting)
            {
                int next = TransmissionLogic.AutoSelectGear(AutoGearIndex, wheelDrivenRpm, config.autoShiftUpRpm,
                    config.autoShiftDownRpm, config.ForwardGearCount, throttle);
                if (next != AutoGearIndex)
                {
                    AutoGearIndex = next;
                    shiftTimer = config.shiftDuration;
                    shifting = true;
                    RaiseGearChanged();
                }
            }

            if (!inGear)
            {
                return 0f;
            }

            float engineTorque = shifting ? 0f : config.torqueCurve.Evaluate(EngineRpm) * throttle;
            if (wheelDrivenRpm >= config.maxRpm)
            {
                engineTorque = 0f; // aylanish cheklagichi
            }

            float wheelTorque = engineTorque * ratio * config.drivetrainEfficiency;

            if (transmission == TransmissionType.Automatic && throttle < 0.05f &&
                Mathf.Abs(ForwardSpeed) < config.creepMaxSpeed)
            {
                wheelTorque += config.creepTorque * (1f - input.brake);
            }

            if (throttle < 0.05f && wheelDrivenRpm > config.idleRpm * 1.2f)
            {
                engineBrakeTorque = config.engineBrakingTorque;
            }

            return wheelTorque * direction;
        }

        private void ApplyWheelForces(float totalWheelTorque, float engineBrakeTorque)
        {
            float frontBrake = input.brake * config.maxBrakeTorque * 2f * config.frontBrakeBias;
            float rearBrake = input.brake * config.maxBrakeTorque * 2f * (1f - config.frontBrakeBias);
            bool parkLock = IsInPark;
            float speedKmh = SpeedKmh;

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null)
                {
                    continue;
                }

                WheelCollider wc = wheel.collider;
                WheelHit hit;
                bool grounded = wc.GetGroundHit(out hit);
                UpdateGrip(wheel, grounded, hit);

                bool driven = IsDriven(wheel);
                float slip = grounded ? Mathf.Abs(hit.forwardSlip) : 0f;

                float motor = driven && drivenWheelCount > 0 ? totalWheelTorque / drivenWheelCount : 0f;
                if (config.tcsEnabled && grounded && Mathf.Abs(motor) > 0.01f && slip > config.tcsSlipThreshold)
                {
                    motor *= config.tcsTorqueFactor;
                }

                wc.motorTorque = motor;

                float brake = wheel.isFront ? frontBrake : rearBrake;
                if (config.absEnabled && brake > 0f && grounded && speedKmh > 5f && slip > config.absSlipThreshold)
                {
                    brake *= config.absReleaseFactor;
                }

                if (!wheel.isFront && HandbrakeEngaged)
                {
                    brake += config.handbrakeTorque;
                }

                if (parkLock)
                {
                    brake += config.parkLockTorque;
                }

                if (driven && engineBrakeTorque > 0f && drivenWheelCount > 0)
                {
                    brake += engineBrakeTorque / drivenWheelCount;
                }

                wc.brakeTorque = brake;
            }
        }

        /// <summary>Yo‘l qoplamasiga (RoadSurface) qarab shina ishqalanishini o‘zgartiradi.</summary>
        private void UpdateGrip(WheelSetup wheel, bool grounded, WheelHit hit)
        {
            float grip = 1f;
            if (grounded && hit.collider != null)
            {
                RoadSurface surface;
                if (hit.collider.TryGetComponent(out surface))
                {
                    grip = surface.gripMultiplier;
                }
            }

            if (Mathf.Abs(grip - wheel.currentGrip) < 0.001f)
            {
                return;
            }

            wheel.currentGrip = grip;
            WheelFrictionCurve forward = wheel.collider.forwardFriction;
            forward.stiffness = config.forwardStiffness * grip;
            wheel.collider.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.collider.sidewaysFriction;
            sideways.stiffness = config.sidewaysStiffness * grip;
            wheel.collider.sidewaysFriction = sideways;
        }

        private void ApplyAerodynamics()
        {
            Vector3 velocity = body.linearVelocity;
            float sqrSpeed = velocity.sqrMagnitude;
            if (sqrSpeed < 0.01f)
            {
                return;
            }

            body.AddForce(-velocity.normalized * (config.aerodynamicDrag * sqrSpeed));
            body.AddForce(-transform.up * (config.downforce * sqrSpeed));
        }

        private void UpdateFuel(float dt)
        {
            if (!EngineRunning)
            {
                return;
            }

            float load = input.throttle * RpmNormalized;
            float litersPerHour = Mathf.Lerp(config.idleFuelLitersPerHour, config.maxFuelLitersPerHour, load);
            FuelLiters = Mathf.Max(0f, FuelLiters - litersPerHour / 3600f * dt);
            if (FuelLiters <= 0f)
            {
                StopEngine();
                GameEvents.Toast("Yoqilg‘i tugadi");
            }
        }

        private bool IsDriven(WheelSetup wheel)
        {
            switch (config.driveType)
            {
                case DriveType.FrontWheelDrive: return wheel.isFront;
                case DriveType.RearWheelDrive: return !wheel.isFront;
                default: return true;
            }
        }

        private float AverageDrivenWheelRpm()
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null || !IsDriven(wheel))
                {
                    continue;
                }

                sum += wheel.collider.rpm;
                count++;
            }

            return count > 0 ? sum / count : 0f;
        }

        private static WheelFrictionCurve MakeCurve(float extremumSlip, float extremumValue, float asymptoteSlip,
            float asymptoteValue, float stiffness)
        {
            return new WheelFrictionCurve
            {
                extremumSlip = extremumSlip,
                extremumValue = extremumValue,
                asymptoteSlip = asymptoteSlip,
                asymptoteValue = asymptoteValue,
                stiffness = stiffness
            };
        }

        // ------------------------------------------------------------------ Vizual

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null || wheel.visual == null)
                {
                    continue;
                }

                Vector3 position;
                Quaternion rotation;
                wheel.collider.GetWorldPose(out position, out rotation);
                wheel.visual.SetPositionAndRotation(position, rotation);
            }

            if (steeringWheelVisual != null)
            {
                float angle = -SteerAngleNormalized * config.steeringWheelMaxDegrees;
                steeringWheelVisual.localRotation = steeringWheelBaseRotation * Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        // ------------------------------------------------------------------ Yordamchi so‘rovlar

        /// <summary>Yerga tegib turgan g‘ildiraklarning tegish nuqtalari (chiziq kesishini aniqlash uchun).</summary>
        public void GetWheelContactPoints(List<Vector3> results)
        {
            results.Clear();
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null)
                {
                    continue;
                }

                WheelHit hit;
                if (wheel.collider.GetGroundHit(out hit))
                {
                    results.Add(hit.point);
                }
            }
        }

        /// <summary>Mashina gabaritining to‘rt burchagi (dunyo koordinatasida). Massiv uzunligi 4 bo‘lishi kerak.</summary>
        public void GetFootprintCorners(Vector3[] corners)
        {
            float halfWidth = config.width * 0.5f;
            float halfLength = config.length * 0.5f;
            Vector3 center = config.footprintCenterOffset + new Vector3(0f, 0.3f, 0f);
            corners[0] = transform.TransformPoint(center + new Vector3(-halfWidth, 0f, halfLength));
            corners[1] = transform.TransformPoint(center + new Vector3(halfWidth, 0f, halfLength));
            corners[2] = transform.TransformPoint(center + new Vector3(-halfWidth, 0f, -halfLength));
            corners[3] = transform.TransformPoint(center + new Vector3(halfWidth, 0f, -halfLength));
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(position, rotation);
            if (body != null)
            {
                body.position = position;
                body.rotation = rotation;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Collided != null)
            {
                Collided.Invoke(collision);
            }
        }
    }
}
