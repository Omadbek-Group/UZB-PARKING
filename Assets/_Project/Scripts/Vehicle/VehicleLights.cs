using System;
using UnityEngine;
using UzbCarParking.Core;

namespace UzbCarParking.Vehicle
{
    public enum HeadlightMode
    {
        Off = 0,
        LowBeam = 1,
        HighBeam = 2
    }

    public enum IndicatorMode
    {
        Off = 0,
        Left = 1,
        Right = 2,
        Hazard = 3
    }

    /// <summary>
    /// Faralar, tormoz va orqaga yurish chiroqlari, burilish signallari va avariya chiroqlari.
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleLights : MonoBehaviour
    {
        [Serializable]
        public class LampGroup
        {
            public Renderer[] renderers = new Renderer[0];
            public Light[] lights = new Light[0];
            public Color offColor = new Color(0.25f, 0.25f, 0.25f);
            public Color onColor = Color.white;
            public float emissionIntensity = 2f;

            [NonSerialized] public Material[] materials;
            [NonSerialized] public float lastLevel = -1f;
        }

        public LampGroup headlights = new LampGroup();
        public LampGroup brakeLights = new LampGroup();
        public LampGroup reverseLights = new LampGroup();
        public LampGroup leftIndicators = new LampGroup();
        public LampGroup rightIndicators = new LampGroup();

        [Header("Sozlamalar")]
        public float blinkInterval = 0.38f;
        public float lowBeamIntensity = 2.5f;
        public float highBeamIntensity = 5f;
        public float lowBeamRange = 25f;
        public float highBeamRange = 55f;
        [Range(0f, 1f)] public float tailLightLevel = 0.35f;
        [Tooltip("Burilishdan keyin rul to‘g‘rilanganda signal o‘zi o‘chadi.")]
        public bool autoCancelIndicators = true;

        public event Action<bool> IndicatorBlink;
        public event Action Changed;

        private VehicleController vehicle;
        private float blinkTimer;
        private bool steeredPastThreshold;
        private bool cached;

        public HeadlightMode Headlights { get; private set; }
        public IndicatorMode Indicator { get; private set; }
        public bool BlinkPhaseOn { get; private set; }

        public bool LeftLampOn
        {
            get { return BlinkPhaseOn && (Indicator == IndicatorMode.Left || Indicator == IndicatorMode.Hazard); }
        }

        public bool RightLampOn
        {
            get { return BlinkPhaseOn && (Indicator == IndicatorMode.Right || Indicator == IndicatorMode.Hazard); }
        }

        private void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }
        }

        private void Start()
        {
            EnsureCached();
        }

        /// <summary>vehicle null bo‘lishi mumkin (garaj ko‘rgazmasi).</summary>
        public void Init(VehicleController vehicleController)
        {
            vehicle = vehicleController;
            cached = false;
            EnsureCached();
        }

        private void EnsureCached()
        {
            if (cached)
            {
                return;
            }

            CacheGroup(headlights);
            CacheGroup(brakeLights);
            CacheGroup(reverseLights);
            CacheGroup(leftIndicators);
            CacheGroup(rightIndicators);
            cached = true;
        }

        private static void CacheGroup(LampGroup group)
        {
            if (group == null)
            {
                return;
            }

            if (group.renderers == null) group.renderers = new Renderer[0];
            if (group.lights == null) group.lights = new Light[0];

            group.materials = new Material[group.renderers.Length];
            for (int i = 0; i < group.renderers.Length; i++)
            {
                // renderer.material — shu chiroq uchun alohida nusxa (rangini mustaqil o‘zgartirish uchun).
                group.materials[i] = group.renderers[i] != null ? group.renderers[i].material : null;
            }

            group.lastLevel = -1f;
        }

        public void CycleHeadlights()
        {
            SetHeadlights((HeadlightMode)(((int)Headlights + 1) % 3));
            switch (Headlights)
            {
                case HeadlightMode.LowBeam: GameEvents.Toast("Faralar: yaqin"); break;
                case HeadlightMode.HighBeam: GameEvents.Toast("Faralar: uzoq"); break;
                default: GameEvents.Toast("Faralar: o‘chiq"); break;
            }
        }

        public void SetHeadlights(HeadlightMode mode)
        {
            Headlights = mode;
            RaiseChanged();
        }

        public void ToggleLeft()
        {
            SetIndicator(Indicator == IndicatorMode.Left ? IndicatorMode.Off : IndicatorMode.Left);
        }

        public void ToggleRight()
        {
            SetIndicator(Indicator == IndicatorMode.Right ? IndicatorMode.Off : IndicatorMode.Right);
        }

        public void ToggleHazard()
        {
            SetIndicator(Indicator == IndicatorMode.Hazard ? IndicatorMode.Off : IndicatorMode.Hazard);
        }

        public void SetIndicator(IndicatorMode mode)
        {
            Indicator = mode;
            blinkTimer = 0f;
            steeredPastThreshold = false;
            BlinkPhaseOn = mode != IndicatorMode.Off;
            if (BlinkPhaseOn && IndicatorBlink != null)
            {
                IndicatorBlink.Invoke(true);
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (Changed != null)
            {
                Changed.Invoke();
            }
        }

        private void Update()
        {
            EnsureCached();

            if (Indicator != IndicatorMode.Off)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer -= blinkInterval;
                    BlinkPhaseOn = !BlinkPhaseOn;
                    if (IndicatorBlink != null)
                    {
                        IndicatorBlink.Invoke(BlinkPhaseOn);
                    }
                }

                AutoCancel();
            }
            else
            {
                BlinkPhaseOn = false;
            }

            float headLevel = Headlights == HeadlightMode.Off ? 0f : (Headlights == HeadlightMode.HighBeam ? 1f : 0.7f);
            bool braking = vehicle != null && vehicle.BrakeInput > 0.05f;
            float brakeLevel = braking ? 1f : (Headlights != HeadlightMode.Off ? tailLightLevel : 0f);
            bool reversing = vehicle != null && vehicle.EngineRunning && vehicle.IsReverseSelected;

            SetLevel(headlights, headLevel);
            SetLevel(brakeLights, brakeLevel);
            SetLevel(reverseLights, reversing ? 1f : 0f);
            SetLevel(leftIndicators, LeftLampOn ? 1f : 0f);
            SetLevel(rightIndicators, RightLampOn ? 1f : 0f);
        }

        private void AutoCancel()
        {
            if (!autoCancelIndicators || vehicle == null)
            {
                return;
            }

            if (Indicator != IndicatorMode.Left && Indicator != IndicatorMode.Right)
            {
                return;
            }

            float direction = Indicator == IndicatorMode.Left ? -1f : 1f;
            float steer = vehicle.SteerAngleNormalized * direction;
            if (steer > 0.5f)
            {
                steeredPastThreshold = true;
            }
            else if (steeredPastThreshold && steer < 0.1f)
            {
                SetIndicator(IndicatorMode.Off);
            }
        }

        private void SetLevel(LampGroup group, float level)
        {
            if (group == null || group.materials == null)
            {
                return;
            }

            if (Mathf.Abs(level - group.lastLevel) < 0.001f)
            {
                return;
            }

            group.lastLevel = level;
            Color baseColor = Color.Lerp(group.offColor, group.onColor, level);
            Color emission = group.onColor * (level * group.emissionIntensity);

            for (int i = 0; i < group.materials.Length; i++)
            {
                RuntimeMaterials.SetColor(group.materials[i], baseColor);
                RuntimeMaterials.SetEmission(group.materials[i], emission);
            }

            bool isHeadlights = group == headlights;
            for (int i = 0; i < group.lights.Length; i++)
            {
                Light lamp = group.lights[i];
                if (lamp == null)
                {
                    continue;
                }

                lamp.enabled = level > 0.01f;
                if (isHeadlights)
                {
                    bool high = Headlights == HeadlightMode.HighBeam;
                    lamp.intensity = high ? highBeamIntensity : lowBeamIntensity;
                    lamp.range = high ? highBeamRange : lowBeamRange;
                }
            }
        }
    }
}
