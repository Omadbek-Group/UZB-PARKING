using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.CameraSystem;
using UzbCarParking.Controls;
using UzbCarParking.Core;
using UzbCarParking.Persistence;
using UzbCarParking.UserInterface;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Avtodrom sahnasining kirish nuqtasi. Sahnada faqat shu komponent turadi; qolgan hamma narsa
    /// (maydon, mashina, kamera, HUD, imtihon) Awake'da yig‘iladi.
    /// </summary>
    public class AvtodromBootstrap : MonoBehaviour
    {
        [Tooltip("Placeholder materiallar (Android build uchun shart).")]
        public MaterialLibrary materialLibrary;
        public VehicleConfig vehicleConfig;
        public ExamConfig examConfig;

        private void Awake()
        {
            SaveData save = SaveSystem.Data;
            RuntimeMaterials.Initialize(materialLibrary);

            VehicleConfig vehicleSettings = vehicleConfig != null
                ? vehicleConfig
                : ScriptableObject.CreateInstance<VehicleConfig>();
            ExamConfig examSettings = examConfig != null
                ? examConfig
                : ScriptableObject.CreateInstance<ExamConfig>();

            Camera cam = SceneSetup.EnsureMainCamera();
            SceneSetup.ApplyOutdoorLighting(cam);

            AvtodromLayout layout = AvtodromBuilder.Build(examSettings);

            // Rejim: to‘liq imtihon yoki bitta mashq.
            List<ExerciseBase> ordered = new List<ExerciseBase>();
            Pose spawn = new Pose(layout.DefaultSpawnPosition, layout.DefaultSpawnRotation);
            string modeId = "full";
            bool fullExam = SessionContext.Mode == AvtodromMode.FullExam ||
                            !layout.Exercises.ContainsKey(SessionContext.ExerciseId);

            if (fullExam)
            {
                for (int i = 0; i < examSettings.examOrder.Count; i++)
                {
                    ExerciseBase exercise;
                    if (layout.Exercises.TryGetValue(examSettings.examOrder[i], out exercise) && !ordered.Contains(exercise))
                    {
                        ordered.Add(exercise);
                    }
                }

                if (ordered.Count == 0)
                {
                    ordered.AddRange(layout.Exercises.Values);
                }
            }
            else
            {
                modeId = SessionContext.ExerciseId;
                ordered.Add(layout.Exercises[modeId]);
                Pose exerciseSpawn;
                if (layout.SpawnPoses.TryGetValue(modeId, out exerciseSpawn))
                {
                    spawn = exerciseSpawn;
                }
            }

            // Mashina.
            VehicleBundle bundle = VehicleSpawner.Spawn(vehicleSettings, spawn.position, spawn.rotation, true);
            if (bundle == null || bundle.Controller == null)
            {
                Debug.LogError("[UZB] Avtodrom: mashinani yaratib bo‘lmadi.");
                return;
            }

            VehicleController vehicle = bundle.Controller;
            vehicle.SetTransmission((TransmissionType)Mathf.Clamp(save.vehicle.transmission, 0, 1));
            if (bundle.Customization != null)
            {
                bundle.Customization.Apply(save.vehicle);
            }

            LineCrossDetector detector = bundle.Root.GetComponent<LineCrossDetector>();
            if (detector == null)
            {
                detector = bundle.Root.AddComponent<LineCrossDetector>();
            }

            detector.Init(vehicle);

            // Kamera.
            CameraController rig = cam.GetComponent<CameraController>();
            if (rig == null)
            {
                rig = cam.gameObject.AddComponent<CameraController>();
            }

            rig.SetTarget(bundle.Root.transform, vehicle.Config != null ? vehicle.Config : vehicleSettings);
            rig.SetMode((CameraMode)Mathf.Clamp(save.settings.cameraMode, 0, 2));

            // Imtihon.
            ExamManager exam = new GameObject("ExamManager").AddComponent<ExamManager>();
            exam.Initialize(examSettings, vehicle, bundle.Lights, detector, bundle.Damage, layout.Beacon,
                ordered, fullExam, modeId);

            // Boshqaruv va HUD.
            MobileInputController input = gameObject.AddComponent<MobileInputController>();
            input.Bind(vehicle, bundle.Lights, rig);

            UIFactory.EnsureEventSystem();
            HudUI.Create(input, vehicle, bundle.Lights, exam, rig, save.settings);
            input.ApplySettings(save.settings);

            GraphicsQuality.Apply(GraphicsQuality.Resolve(save.settings.graphicsLevel));
            AudioListener.volume = save.settings.masterVolume;
        }
    }
}
