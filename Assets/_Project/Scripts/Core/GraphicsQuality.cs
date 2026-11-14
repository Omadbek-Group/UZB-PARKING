using UnityEngine;
using UnityEngine.Rendering;
#if UZB_HAS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UzbCarParking.Core
{
    public enum GraphicsLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// Android uchun Low / Medium / High grafik profillari.
    /// Maqsad: Low va Medium — barqaror 30 FPS, High — kuchli qurilmalarda 60 FPS.
    /// </summary>
    public static class GraphicsQuality
    {
        public static GraphicsLevel Current { get; private set; } = GraphicsLevel.Medium;

        /// <summary>savedLevel &lt; 0 bo‘lsa qurilmaga qarab avtomatik tanlanadi.</summary>
        public static GraphicsLevel Resolve(int savedLevel)
        {
            if (savedLevel >= 0 && savedLevel <= 2)
            {
                return (GraphicsLevel)savedLevel;
            }

            return RecommendForDevice();
        }

        public static GraphicsLevel RecommendForDevice()
        {
            if (!Application.isMobilePlatform)
            {
                return GraphicsLevel.High;
            }

            int memoryMb = SystemInfo.systemMemorySize;
            if (memoryMb < 3500)
            {
                return GraphicsLevel.Low;
            }

            return memoryMb < 7000 ? GraphicsLevel.Medium : GraphicsLevel.High;
        }

        public static void Apply(GraphicsLevel level)
        {
            Current = level;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = level == GraphicsLevel.High ? 60 : 30;
            QualitySettings.lodBias = level == GraphicsLevel.Low ? 0.6f : (level == GraphicsLevel.Medium ? 1f : 1.5f);

            float shadowDistance = level == GraphicsLevel.Low ? 0f : (level == GraphicsLevel.Medium ? 35f : 60f);
            QualitySettings.shadowDistance = shadowDistance;

#if UZB_HAS_URP
            UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                CacheEditorDefaults(urp);
                urp.renderScale = level == GraphicsLevel.Low ? 0.75f : (level == GraphicsLevel.Medium ? 0.9f : 1f);
                urp.shadowDistance = shadowDistance;
                urp.msaaSampleCount = level == GraphicsLevel.Low ? 1 : (level == GraphicsLevel.Medium ? 2 : 4);
            }
#endif

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional)
                {
                    continue;
                }

                lights[i].shadows = level == GraphicsLevel.Low
                    ? LightShadows.None
                    : (level == GraphicsLevel.Medium ? LightShadows.Hard : LightShadows.Soft);
            }
        }

#if UZB_HAS_URP
#if UNITY_EDITOR
        // Editor'da URP asset qiymatlari diskka yozilib qolmasligi uchun Play rejimidan chiqishda tiklanadi.
        private static UniversalRenderPipelineAsset cachedAsset;
        private static float cachedRenderScale;
        private static float cachedShadowDistance;
        private static int cachedMsaa;

        private static void CacheEditorDefaults(UniversalRenderPipelineAsset urp)
        {
            if (cachedAsset == urp)
            {
                return;
            }

            cachedAsset = urp;
            cachedRenderScale = urp.renderScale;
            cachedShadowDistance = urp.shadowDistance;
            cachedMsaa = urp.msaaSampleCount;
            Application.quitting -= RestoreEditorDefaults;
            Application.quitting += RestoreEditorDefaults;
        }

        private static void RestoreEditorDefaults()
        {
            Application.quitting -= RestoreEditorDefaults;
            if (cachedAsset == null)
            {
                return;
            }

            cachedAsset.renderScale = cachedRenderScale;
            cachedAsset.shadowDistance = cachedShadowDistance;
            cachedAsset.msaaSampleCount = cachedMsaa;
            cachedAsset = null;
        }
#else
        private static void CacheEditorDefaults(UniversalRenderPipelineAsset urp)
        {
        }
#endif
#endif
    }
}
