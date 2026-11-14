using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Garage;

namespace UzbCarParking.Persistence
{
    [Serializable]
    public class PlayerProfileData
    {
        public string playerName = "Haydovchi";
        public int examsTaken;
        public int examsPassed;
    }

    [Serializable]
    public class VehicleCustomizationData
    {
        public string vehicleId = "placeholder_hatchback";
        public string paintColorHex = "#E8E8E8";
        public string rimColorHex = "#9EA3AB";
        public string plateRegion = "01";
        public string plateNumber = "A777AA";

        /// <summary>0 — avtomat, 1 — mexanik.</summary>
        public int transmission;
    }

    [Serializable]
    public class GameSettingsData
    {
        /// <summary>-1 — avtomatik, 0 — past, 1 — o‘rta, 2 — yuqori.</summary>
        public int graphicsLevel = -1;

        /// <summary>0 — rul g‘ildiragi, 1 — chap/o‘ng tugmalar.</summary>
        public int steeringMode;

        public float steeringSensitivity = 1f;
        public float masterVolume = 0.8f;
        public float controlsOpacity = 0.85f;
        public float controlsScale = 1f;
        public int cameraMode;
        public int language;
    }

    [Serializable]
    public class ExamResultData
    {
        public string dateUtc = string.Empty;
        public string mode = string.Empty;
        public int score;
        public bool passed;
        public float durationSeconds;
        public List<string> penalties = new List<string>();
    }

    /// <summary>Lokal saqlanadigan barcha ma'lumotlar (JSON).</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;
        public const int MaxStoredResults = 20;

        public int version = CurrentVersion;
        public PlayerProfileData profile = new PlayerProfileData();
        public VehicleCustomizationData vehicle = new VehicleCustomizationData();
        public GameSettingsData settings = new GameSettingsData();
        public List<ExamResultData> examResults = new List<ExamResultData>();
        public int bestExamScore = -1;

        // Faqat o‘yin ichida amal qiladigan virtual guvohnoma. Haqiqiy hujjat emas.
        public bool hasVirtualLicense;
        public string virtualLicenseNumber = string.Empty;
        public string virtualLicenseIssuedUtc = string.Empty;

        /// <summary>Buzilgan yoki eski fayldan o‘qilgan qiymatlarni xavfsiz diapazonga keltiradi.</summary>
        public void Validate()
        {
            version = CurrentVersion;

            if (profile == null) profile = new PlayerProfileData();
            if (vehicle == null) vehicle = new VehicleCustomizationData();
            if (settings == null) settings = new GameSettingsData();
            if (examResults == null) examResults = new List<ExamResultData>();

            if (string.IsNullOrWhiteSpace(profile.playerName)) profile.playerName = "Haydovchi";
            profile.examsTaken = Mathf.Max(0, profile.examsTaken);
            profile.examsPassed = Mathf.Clamp(profile.examsPassed, 0, profile.examsTaken);

            if (string.IsNullOrEmpty(vehicle.vehicleId)) vehicle.vehicleId = "placeholder_hatchback";
            if (string.IsNullOrEmpty(vehicle.paintColorHex)) vehicle.paintColorHex = "#E8E8E8";
            if (string.IsNullOrEmpty(vehicle.rimColorHex)) vehicle.rimColorHex = "#9EA3AB";
            if (!PlateNumber.IsValidRegion(vehicle.plateRegion)) vehicle.plateRegion = "01";
            vehicle.plateNumber = PlateNumber.IsValidNumber(vehicle.plateNumber)
                ? PlateNumber.Normalize(vehicle.plateNumber)
                : "A777AA";
            vehicle.transmission = Mathf.Clamp(vehicle.transmission, 0, 1);

            settings.graphicsLevel = Mathf.Clamp(settings.graphicsLevel, -1, 2);
            settings.steeringMode = Mathf.Clamp(settings.steeringMode, 0, 1);
            settings.steeringSensitivity = Mathf.Clamp(settings.steeringSensitivity, 0.5f, 2f);
            settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
            settings.controlsOpacity = Mathf.Clamp(settings.controlsOpacity, 0.3f, 1f);
            settings.controlsScale = Mathf.Clamp(settings.controlsScale, 0.7f, 1.3f);
            settings.cameraMode = Mathf.Clamp(settings.cameraMode, 0, 2);
            settings.language = Mathf.Max(0, settings.language);

            examResults.RemoveAll(r => r == null);
            while (examResults.Count > MaxStoredResults)
            {
                examResults.RemoveAt(0);
            }

            bestExamScore = Mathf.Clamp(bestExamScore, -1, 100);
            if (virtualLicenseNumber == null) virtualLicenseNumber = string.Empty;
            if (virtualLicenseIssuedUtc == null) virtualLicenseIssuedUtc = string.Empty;
            if (!hasVirtualLicense)
            {
                virtualLicenseNumber = string.Empty;
                virtualLicenseIssuedUtc = string.Empty;
            }
        }
    }
}
