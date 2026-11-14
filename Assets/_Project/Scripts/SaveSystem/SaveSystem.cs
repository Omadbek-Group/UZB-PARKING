using System;
using System.IO;
using UnityEngine;

namespace UzbCarParking.Persistence
{
    /// <summary>
    /// Lokal saqlash: Application.persistentDataPath ichida JSON fayl.
    /// Yozish atomik (avval .tmp), oldingi nusxa .bak sifatida saqlanadi.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "uzb_car_parking_save.json";

        private static SaveData data;
        private static string pathOverride;

        public static event Action<SaveData> Saved;

        public static string FilePath
        {
            get
            {
                return string.IsNullOrEmpty(pathOverride)
                    ? Path.Combine(Application.persistentDataPath, FileName)
                    : pathOverride;
            }
        }

        /// <summary>Joriy ma'lumotlar. Birinchi murojaatda diskdan o‘qiladi.</summary>
        public static SaveData Data
        {
            get
            {
                if (data == null)
                {
                    Load();
                }

                return data;
            }
        }

        /// <summary>Faqat testlar uchun: saqlash yo‘lini almashtiradi. null — standart yo‘lga qaytaradi.</summary>
        public static void SetPathOverrideForTests(string path)
        {
            pathOverride = path;
            data = null;
        }

        public static SaveData Load()
        {
            string path = FilePath;
            SaveData loaded = TryRead(path);
            if (loaded == null)
            {
                loaded = TryRead(path + ".bak");
                if (loaded != null)
                {
                    Debug.LogWarning("[UZB] Asosiy saqlash fayli o‘qilmadi, zaxira nusxa (.bak) tiklandi.");
                }
            }

            data = loaded ?? new SaveData();
            data.Validate();
            return data;
        }

        public static bool Save()
        {
            if (data == null)
            {
                data = new SaveData();
            }

            try
            {
                data.Validate();
                string json = JsonUtility.ToJson(data, true);
                string path = FilePath;
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }

                File.Move(tempPath, path);

                if (Saved != null)
                {
                    Saved.Invoke(data);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[UZB] Saqlashda xato: " + exception.Message);
                return false;
            }
        }

        public static void AddExamResult(ExamResultData result)
        {
            if (result == null)
            {
                return;
            }

            SaveData current = Data;
            current.examResults.Add(result);
            while (current.examResults.Count > SaveData.MaxStoredResults)
            {
                current.examResults.RemoveAt(0);
            }
        }

        /// <summary>Barcha progressni o‘chiradi. Chaqirishdan oldin foydalanuvchidan tasdiq oling.</summary>
        public static void ResetAll()
        {
            data = new SaveData();
            Save();
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[UZB] Saqlash faylini o‘qib bo‘lmadi (" + path + "): " + exception.Message);
                return null;
            }
        }
    }
}
