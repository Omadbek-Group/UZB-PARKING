using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.AudioSystem
{
    /// <summary>
    /// Dvigatel, signal (gudok), burilish signali chertishi va zarba tovushlari.
    /// AudioClip maydonlari bo‘sh qolsa, ProceduralAudio'dagi vaqtinchalik sintez ishlatiladi.
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleAudio : MonoBehaviour
    {
        [Header("Haqiqiy audio fayllar (ixtiyoriy)")]
        public AudioClip engineLoopClip;
        public AudioClip hornClip;
        public AudioClip indicatorOnClip;
        public AudioClip indicatorOffClip;
        public AudioClip starterClip;
        public AudioClip impactClip;

        [Header("Dvigatel tovushi")]
        public float minPitch = 0.55f;
        public float maxPitch = 2.4f;
        public float idleVolume = 0.25f;
        public float loadVolume = 0.7f;

        private VehicleController vehicle;
        private VehicleLights lights;
        private AudioSource engineSource;
        private AudioSource hornSource;
        private AudioSource effectsSource;
        private bool ready;

        public void Init(VehicleController vehicleController, VehicleLights vehicleLights)
        {
            Unsubscribe();
            vehicle = vehicleController;
            lights = vehicleLights;
            if (vehicle == null)
            {
                return;
            }

            if (engineSource == null)
            {
                engineSource = CreateSource("EngineAudio", true);
                hornSource = CreateSource("HornAudio", true);
                effectsSource = CreateSource("EffectsAudio", false);
            }

            engineSource.clip = engineLoopClip != null ? engineLoopClip : ProceduralAudio.EngineLoop;
            hornSource.clip = hornClip != null ? hornClip : ProceduralAudio.Horn;
            hornSource.volume = 0.8f;

            vehicle.EngineStateChanged += OnEngineStateChanged;
            vehicle.Collided += OnCollided;
            if (lights != null)
            {
                lights.IndicatorBlink += OnIndicatorBlink;
            }

            ready = true;
            if (vehicle.EngineRunning)
            {
                engineSource.Play();
            }
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0.5f;
            source.minDistance = 4f;
            source.maxDistance = 60f;
            source.dopplerLevel = 0f;
            return source;
        }

        private void Update()
        {
            if (!ready || vehicle == null)
            {
                return;
            }

            if (engineSource.isPlaying)
            {
                float rpm = vehicle.RpmNormalized;
                engineSource.pitch = Mathf.Lerp(minPitch, maxPitch, rpm);
                float load = Mathf.Max(vehicle.ThrottleInput, rpm * 0.5f);
                engineSource.volume = Mathf.Lerp(idleVolume, loadVolume, load);
            }

            if (vehicle.HornActive && !hornSource.isPlaying)
            {
                hornSource.Play();
            }
            else if (!vehicle.HornActive && hornSource.isPlaying)
            {
                hornSource.Stop();
            }
        }

        private void OnEngineStateChanged(bool running)
        {
            if (running)
            {
                effectsSource.PlayOneShot(starterClip != null ? starterClip : ProceduralAudio.Starter, 0.7f);
                engineSource.Play();
            }
            else
            {
                engineSource.Stop();
            }
        }

        private void OnIndicatorBlink(bool on)
        {
            AudioClip clip = on
                ? (indicatorOnClip != null ? indicatorOnClip : ProceduralAudio.TickOn)
                : (indicatorOffClip != null ? indicatorOffClip : ProceduralAudio.TickOff);
            effectsSource.PlayOneShot(clip, 0.5f);
        }

        private void OnCollided(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < 1.5f)
            {
                return;
            }

            effectsSource.PlayOneShot(impactClip != null ? impactClip : ProceduralAudio.Impact,
                Mathf.Clamp01(speed / 10f));
        }

        private void Unsubscribe()
        {
            if (vehicle != null)
            {
                vehicle.EngineStateChanged -= OnEngineStateChanged;
                vehicle.Collided -= OnCollided;
            }

            if (lights != null)
            {
                lights.IndicatorBlink -= OnIndicatorBlink;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
