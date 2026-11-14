using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UzbCarParking.Vehicle;

namespace UzbCarParking.EditorTools
{
    /// <summary>
    /// Blender modellarini sahnalarga qo‘yadi: O‘zbekiston xaritasi (MainMenu), eshikli mashina va personajlar (Avtodrom).
    /// Qayta ishga tushirilsa, eski nomli obyektlarni almashtiradi.
    /// </summary>
    public static class UzbWorldSetup
    {
        private const string ModelsFolder = "Assets/_Project/Art/Models/";
        private const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string AvtodromPath = "Assets/_Project/Scenes/Avtodrom.unity";

        [MenuItem("UZB Car Parking/5. Modellarni sahnaga qo‘shish")]
        public static void AddModels()
        {
            Scene menu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Place("Uzbekistan_Map", "uzbekistan_map.fbx", new Vector3(0f, -0.2f, 0f), 0.01f);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);

            Scene avto = EditorSceneManager.OpenScene(AvtodromPath, OpenSceneMode.Single);
            GameObject car = Place("Showroom_Car", "car_spark.fbx", new Vector3(20f, 0f, 0f), 1f);
            if (car != null && car.GetComponent<CarDoor>() == null)
            {
                car.AddComponent<CarDoor>();
            }
            Place("Char_Male", "char_male.fbx", new Vector3(24f, 0f, 0f), 1f);
            Place("Char_Female", "char_female.fbx", new Vector3(26f, 0f, 0f), 1f);
            EditorSceneManager.MarkSceneDirty(avto);
            EditorSceneManager.SaveScene(avto);

            AssetDatabase.SaveAssets();
            Debug.Log("UZB_WORLD_OK");
        }

        private static GameObject Place(string name, string file, Vector3 position, float scale)
        {
            GameObject old = GameObject.Find(name);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + file);
            if (prefab == null)
            {
                Debug.LogError("UZB_WORLD: model topilmadi: " + ModelsFolder + file);
                return null;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
    }
}
