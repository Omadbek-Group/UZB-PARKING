using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.Garage;
using UzbCarParking.Persistence;
using UzbCarParking.Vehicle;

namespace UzbCarParking.UserInterface
{
    /// <summary>
    /// Bosh menyu sahnasining kirish nuqtasi: garaj, kamera va menyu interfeysini yig‘adi.
    /// </summary>
    public class MainMenuBootstrap : MonoBehaviour
    {
        [Tooltip("Placeholder materiallar (Android build uchun shart).")]
        public MaterialLibrary materialLibrary;
        public VehicleConfig vehicleConfig;

        private void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            SaveData save = SaveSystem.Data;
            RuntimeMaterials.Initialize(materialLibrary);

            VehicleConfig vehicleSettings = vehicleConfig != null
                ? vehicleConfig
                : ScriptableObject.CreateInstance<VehicleConfig>();

            Camera cam = SceneSetup.EnsureMainCamera();
            SceneSetup.ApplyGarageLighting(cam);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.2f;

            // Mashina ekranning o‘ng tomonida ko‘rinadi, chap tomon — menyu uchun.
            Vector3 cameraPosition = new Vector3(-2.2f, 1.7f, -7.2f);
            Vector3 lookPoint = new Vector3(-1.9f, 0.75f, 0f);
            cam.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(lookPoint - cameraPosition));

            GarageShowroom showroom = GarageShowroom.Build(vehicleSettings, save.vehicle);

            UIFactory.EnsureEventSystem();
            MainMenuUI.Create(showroom);

            GraphicsQuality.Apply(GraphicsQuality.Resolve(save.settings.graphicsLevel));
            AudioListener.volume = save.settings.masterVolume;
        }
    }
}
