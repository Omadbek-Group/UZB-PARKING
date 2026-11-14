using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UzbCarParking.Avtodrom;
using UzbCarParking.Core;
using UzbCarParking.Persistence;
using UzbCarParking.UserInterface;
using UzbCarParking.Vehicle;

namespace UzbCarParking.EditorTools
{
    /// <summary>
    /// "UZB Car Parking" menyusi: loyihani bir bosishda sozlash va Android build.
    /// Mavjud assetlar ustidan yozilmaydi; sahnani qayta yaratishdan oldin tasdiq so‘raladi.
    /// </summary>
    public static class UzbProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string MaterialsFolder = Root + "/Materials";
        private const string ScenesFolder = Root + "/Scenes";
        private const string LibraryPath = Root + "/ScriptableObjects/MaterialLibrary.asset";
        private const string VehicleConfigPath = Root + "/ScriptableObjects/Vehicles/VC_PlaceholderHatchback.asset";
        private const string ExamConfigPath = Root + "/ScriptableObjects/Avtodrom/EC_AvtodromDefault.asset";
        private const string MainMenuScenePath = ScenesFolder + "/" + GameConstants.MainMenuScene + ".unity";
        private const string AvtodromScenePath = ScenesFolder + "/" + GameConstants.AvtodromScene + ".unity";

        private static readonly string[] Folders =
        {
            Root + "/Prefabs",
            Root + "/Prefabs/Vehicles",
            Root + "/Scenes",
            Root + "/Materials",
            Root + "/Models",
            Root + "/Textures",
            Root + "/Audio",
            Root + "/UI",
            Root + "/ScriptableObjects",
            Root + "/ScriptableObjects/Vehicles",
            Root + "/ScriptableObjects/Avtodrom"
        };

        // ------------------------------------------------------------------ 1. Sozlash

        [MenuItem("UZB Car Parking/1. MVP loyihani sozlash", priority = 1)]
        public static void SetupProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("UZB Car Parking", "Avval Play rejimidan chiqing.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (GraphicsSettingsHasNoPipeline())
            {
                bool proceed = EditorUtility.DisplayDialog("UZB Car Parking",
                    "Loyihada Render Pipeline (URP) asset o‘rnatilmagan ko‘rinadi.\n\n" +
                    "Tavsiya: Unity Hub'da 'Universal 3D' shabloni bilan yangi loyiha yarating va _Project " +
                    "papkasini o‘sha loyihaga ko‘chiring.\n\nShunga qaramay davom etilsinmi?",
                    "Davom etish", "Bekor qilish");
                if (!proceed)
                {
                    return;
                }
            }

            EnsureFolders();
            MaterialLibrary library = EnsureMaterialLibrary();
            VehicleConfig vehicleConfig = EnsureAsset<VehicleConfig>(VehicleConfigPath);
            ExamConfig examConfig = EnsureAsset<ExamConfig>(ExamConfigPath);
            AssetDatabase.SaveAssets();

            CreateScene(MainMenuScenePath, root =>
            {
                MainMenuBootstrap bootstrap = root.AddComponent<MainMenuBootstrap>();
                bootstrap.materialLibrary = library;
                bootstrap.vehicleConfig = vehicleConfig;
            });

            CreateScene(AvtodromScenePath, root =>
            {
                AvtodromBootstrap bootstrap = root.AddComponent<AvtodromBootstrap>();
                bootstrap.materialLibrary = library;
                bootstrap.vehicleConfig = vehicleConfig;
                bootstrap.examConfig = examConfig;
            });

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(AvtodromScenePath, true)
            };

            ConfigureAndroidPlayerSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (File.Exists(MainMenuScenePath))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("UZB Car Parking",
                "Sozlash tugadi.\n\n" +
                "• Sahnalar: MainMenu, Avtodrom (Build Settings'ga qo‘shildi)\n" +
                "• Assetlar: MaterialLibrary, VehicleConfig, ExamConfig\n" +
                "• Android Player Settings sozlandi\n\n" +
                "Endi Play tugmasini bosing.", "OK");
        }

        private static bool GraphicsSettingsHasNoPipeline()
        {
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null;
        }

        private static void EnsureFolders()
        {
            for (int i = 0; i < Folders.Length; i++)
            {
                EnsureFolder(Folders[i]);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static MaterialLibrary EnsureMaterialLibrary()
        {
            MaterialLibrary library = EnsureAsset<MaterialLibrary>(LibraryPath);

            foreach (MaterialKind kind in Enum.GetValues(typeof(MaterialKind)))
            {
                string path = MaterialsFolder + "/M_" + kind + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = RuntimeMaterials.CreateMaterial(kind);
                    AssetDatabase.CreateAsset(material, path);
                }

                library.Set(kind, material);
            }

            EditorUtility.SetDirty(library);
            return library;
        }

        private static void CreateScene(string path, Action<GameObject> configureRoot)
        {
            if (File.Exists(path))
            {
                bool recreate = EditorUtility.DisplayDialog("UZB Car Parking",
                    "'" + path + "' sahnasi allaqachon mavjud.\n\nQayta yaratilsinmi? (Sahnadagi qo‘lda kiritilgan " +
                    "o‘zgarishlar yo‘qoladi.)", "Qayta yaratish", "Mavjudini qoldirish");
                if (!recreate)
                {
                    return;
                }
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject root = new GameObject("GameRoot");
            configureRoot(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ------------------------------------------------------------------ Android

        [MenuItem("UZB Car Parking/2. Android sozlamalarini qo‘llash", priority = 2)]
        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "UZB Games";
            PlayerSettings.productName = "UZB Car Parking";
            PlayerSettings.bundleVersion = "0.1.0";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "uz.uzbgames.carparking");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;

            Debug.Log("[UZB] Android Player Settings qo‘llandi: IL2CPP, ARM64, min API 26, landshaft, " +
                      "paket: uz.uzbgames.carparking. Relizdan oldin kompaniya nomi va paket identifikatorini o‘zingiznikiga almashtiring.");
        }

        [MenuItem("UZB Car Parking/3. Android APK yig‘ish (ichki test)", priority = 20)]
        public static void BuildAndroidApk()
        {
            BuildAndroid(false);
        }

        [MenuItem("UZB Car Parking/4. Android AAB yig‘ish (Google Play)", priority = 21)]
        public static void BuildAndroidAab()
        {
            BuildAndroid(true);
        }

        private static void BuildAndroid(bool appBundle)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("UZB Car Parking",
                    "Android Build Support o‘rnatilmagan.\nUnity Hub ▸ Installs ▸ Add modules ▸ Android Build Support " +
                    "(OpenJDK va Android SDK & NDK bilan).", "OK");
                return;
            }

            if (!File.Exists(MainMenuScenePath) || !File.Exists(AvtodromScenePath))
            {
                EditorUtility.DisplayDialog("UZB Car Parking",
                    "Sahnalar topilmadi. Avval '1. MVP loyihani sozlash' ni ishga tushiring.", "OK");
                return;
            }

            string directory = "Builds/Android";
            Directory.CreateDirectory(directory);
            EditorUserBuildSettings.buildAppBundle = appBundle;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainMenuScenePath, AvtodromScenePath },
                locationPathName = directory + "/UzbCarParking." + (appBundle ? "aab" : "apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[UZB] Build tayyor: " + summary.outputPath + " (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB)");
                EditorUtility.RevealInFinder(summary.outputPath);
            }
            else
            {
                Debug.LogError("[UZB] Build muvaffaqiyatsiz: " + summary.result + ", xatolar: " + summary.totalErrors);
            }
        }

        // ------------------------------------------------------------------ Yordamchi

        [MenuItem("UZB Car Parking/Saqlash papkasini ochish", priority = 40)]
        public static void RevealSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("UZB Car Parking/Saqlangan progressni o‘chirish...", priority = 41)]
        public static void DeleteSave()
        {
            string path = SaveSystem.FilePath;
            bool confirmed = EditorUtility.DisplayDialog("UZB Car Parking",
                "Saqlangan progress o‘chirilsinmi?\n\n" + path + "\n\nBu amalni ortga qaytarib bo‘lmaydi.",
                "O‘chirish", "Bekor qilish");
            if (!confirmed)
            {
                return;
            }

            DeleteIfExists(path);
            DeleteIfExists(path + ".bak");
            DeleteIfExists(path + ".tmp");
            SaveSystem.SetPathOverrideForTests(null);
            Debug.Log("[UZB] Saqlangan progress o‘chirildi.");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
