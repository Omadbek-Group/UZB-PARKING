using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UzbCarParking.Persistence;

namespace UzbCarParking.Tests
{
    /// <summary>Testlar vaqtinchalik papkada ishlaydi — foydalanuvchining haqiqiy saqlash fayliga tegmaydi.</summary>
    public class SaveSystemTests
    {
        private string directory;
        private string path;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Application.temporaryCachePath, "uzb_save_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "save.json");
            SaveSystem.SetPathOverrideForTests(path);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.SetPathOverrideForTests(null);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void MissingFileGivesDefaults()
        {
            SaveData data = SaveSystem.Load();

            Assert.IsNotNull(data);
            Assert.AreEqual("01", data.vehicle.plateRegion);
            Assert.AreEqual(-1, data.bestExamScore);
            Assert.IsFalse(data.hasVirtualLicense);
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            SaveData data = SaveSystem.Data;
            data.vehicle.paintColorHex = "#C0392B";
            data.vehicle.plateRegion = "30";
            data.vehicle.plateNumber = "B123CD";
            data.vehicle.transmission = 1;
            data.settings.graphicsLevel = 2;
            data.settings.steeringSensitivity = 1.5f;
            data.bestExamScore = 95;
            data.hasVirtualLicense = true;
            data.virtualLicenseNumber = "UZB-VIRT-123456";
            ExamResultData result = new ExamResultData { dateUtc = "2026-01-01T00:00:00Z", mode = "full", score = 95, passed = true };
            result.penalties.Add("Konusga tegildi (-5)");
            SaveSystem.AddExamResult(result);

            Assert.IsTrue(SaveSystem.Save());
            Assert.IsTrue(File.Exists(path));

            SaveSystem.SetPathOverrideForTests(path); // keshni tozalaydi
            SaveData loaded = SaveSystem.Load();

            Assert.AreEqual("#C0392B", loaded.vehicle.paintColorHex);
            Assert.AreEqual("30", loaded.vehicle.plateRegion);
            Assert.AreEqual("B123CD", loaded.vehicle.plateNumber);
            Assert.AreEqual(1, loaded.vehicle.transmission);
            Assert.AreEqual(2, loaded.settings.graphicsLevel);
            Assert.AreEqual(1.5f, loaded.settings.steeringSensitivity, 0.001f);
            Assert.AreEqual(95, loaded.bestExamScore);
            Assert.IsTrue(loaded.hasVirtualLicense);
            Assert.AreEqual("UZB-VIRT-123456", loaded.virtualLicenseNumber);
            Assert.AreEqual(1, loaded.examResults.Count);
            Assert.AreEqual(1, loaded.examResults[0].penalties.Count);
        }

        [Test]
        public void SecondSaveKeepsBackup()
        {
            SaveSystem.Data.bestExamScore = 70;
            SaveSystem.Save();
            SaveSystem.Data.bestExamScore = 90;
            SaveSystem.Save();

            Assert.IsTrue(File.Exists(path + ".bak"));
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void CorruptedFileFallsBackToBackup()
        {
            SaveSystem.Data.bestExamScore = 88;
            SaveSystem.Save();
            SaveSystem.Save(); // .bak yaratiladi
            File.WriteAllText(path, "{ bu json emas ");

            LogAssert.ignoreFailingMessages = true;
            SaveSystem.SetPathOverrideForTests(path);
            SaveData loaded = SaveSystem.Load();
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(88, loaded.bestExamScore);
        }

        [Test]
        public void ValidateClampsBadValues()
        {
            SaveData data = new SaveData();
            data.settings.graphicsLevel = 99;
            data.settings.steeringSensitivity = 50f;
            data.settings.masterVolume = -3f;
            data.settings.controlsOpacity = 0f;
            data.vehicle.plateRegion = "zz";
            data.vehicle.plateNumber = "???";
            data.vehicle.transmission = 7;
            data.profile = null;
            data.examResults = null;
            data.hasVirtualLicense = false;
            data.virtualLicenseNumber = "eski";

            data.Validate();

            Assert.AreEqual(2, data.settings.graphicsLevel);
            Assert.AreEqual(2f, data.settings.steeringSensitivity, 0.001f);
            Assert.AreEqual(0f, data.settings.masterVolume, 0.001f);
            Assert.AreEqual(0.3f, data.settings.controlsOpacity, 0.001f);
            Assert.AreEqual("01", data.vehicle.plateRegion);
            Assert.AreEqual("A777AA", data.vehicle.plateNumber);
            Assert.AreEqual(1, data.vehicle.transmission);
            Assert.IsNotNull(data.profile);
            Assert.IsNotNull(data.examResults);
            Assert.AreEqual(string.Empty, data.virtualLicenseNumber);
        }

        [Test]
        public void ResultHistoryIsCapped()
        {
            for (int i = 0; i < SaveData.MaxStoredResults + 15; i++)
            {
                SaveSystem.AddExamResult(new ExamResultData { score = i });
            }

            Assert.AreEqual(SaveData.MaxStoredResults, SaveSystem.Data.examResults.Count);
            Assert.AreEqual(SaveData.MaxStoredResults + 14, SaveSystem.Data.examResults[SaveData.MaxStoredResults - 1].score);
        }
    }
}
