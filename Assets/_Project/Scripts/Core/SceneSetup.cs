using UnityEngine;
using UnityEngine.Rendering;

namespace UzbCarParking.Core
{
    /// <summary>Sahnadagi kamera, quyosh va muhit yoritilishini tayyorlaydi (mavjud bo‘lsa — qayta ishlatadi).</summary>
    public static class SceneSetup
    {
        public static Camera EnsureMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            return cam;
        }

        public static Light EnsureSun()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            GameObject go = new GameObject("Directional Light");
            Light sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            return sun;
        }

        /// <summary>Kunduzgi ochiq maydon yoritilishi (avtodrom).</summary>
        public static void ApplyOutdoorLighting(Camera cam)
        {
            Light sun = EnsureSun();
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;

            Color horizon = new Color(0.72f, 0.82f, 0.92f);
            SetFlatAmbient(new Color(0.42f, 0.46f, 0.52f));

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = horizon;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 320f;

            if (cam != null)
            {
                cam.farClipPlane = 450f;
                cam.backgroundColor = horizon;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            }
        }

        /// <summary>Qorong‘i garaj yoritilishi (bosh menyu).</summary>
        public static void ApplyGarageLighting(Camera cam)
        {
            Light sun = EnsureSun();
            sun.transform.rotation = Quaternion.Euler(60f, 30f, 0f);
            sun.intensity = 0.35f;
            sun.color = new Color(0.8f, 0.88f, 1f);
            sun.shadows = LightShadows.None;

            SetFlatAmbient(new Color(0.2f, 0.22f, 0.27f));
            RenderSettings.fog = false;

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.035f, 0.045f, 0.07f);
                cam.farClipPlane = 120f;
            }
        }

        private static void SetFlatAmbient(Color color)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = color;

            // URP muhit yorug‘ligini ambientProbe'dan oladi — pishirilgan (baked) ma'lumot bo‘lmasa ham ishlashi uchun.
            SphericalHarmonicsL2 probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(color);
            RenderSettings.ambientProbe = probe;
        }
    }
}
