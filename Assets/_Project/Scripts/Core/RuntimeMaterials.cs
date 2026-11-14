using System.Collections.Generic;
using UnityEngine;

namespace UzbCarParking.Core
{
    /// <summary>
    /// Placeholder materiallarni beradi. Avval MaterialLibrary'dan oladi,
    /// topilmasa URP Lit asosida vaqtinchalik material yaratadi (faqat Editor'da ishonchli).
    /// </summary>
    public static class RuntimeMaterials
    {
        public struct Spec
        {
            public Color color;
            public float metallic;
            public float smoothness;
            public bool emissive;

            public Spec(Color color, float metallic, float smoothness, bool emissive = false)
            {
                this.color = color;
                this.metallic = metallic;
                this.smoothness = smoothness;
                this.emissive = emissive;
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");

        private static readonly Dictionary<MaterialKind, Material> Cache = new Dictionary<MaterialKind, Material>();
        private static MaterialLibrary library;

        public static void Initialize(MaterialLibrary materialLibrary)
        {
            library = materialLibrary;
            Cache.Clear();
            if (library == null)
            {
                Debug.LogWarning("[UZB] MaterialLibrary biriktirilmagan. Vaqtinchalik materiallar yaratiladi; " +
                                 "Android build uchun 'UZB Car Parking ▸ 1. MVP loyihani sozlash' menyusini ishga tushiring.");
            }
        }

        /// <summary>Umumiy (shared) material. O‘zgartirmang — nusxa uchun CreateInstance ishlating.</summary>
        public static Material Get(MaterialKind kind)
        {
            if (Cache.TryGetValue(kind, out Material cached) && cached != null)
            {
                return cached;
            }

            Material material = library != null ? library.Get(kind) : null;
            if (material == null)
            {
                material = CreateMaterial(kind);
            }

            Cache[kind] = material;
            return material;
        }

        public static Material CreateInstance(MaterialKind kind, Color color)
        {
            Material instance = new Material(Get(kind));
            instance.name = "M_" + kind + " (Instance)";
            SetColor(instance, color);
            return instance;
        }

        public static Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            return shader;
        }

        /// <summary>Yangi material yaratadi. Editor sozlash vositasi ham shu metoddan foydalanadi.</summary>
        public static Material CreateMaterial(MaterialKind kind)
        {
            Spec spec = GetSpec(kind);
            Material material = new Material(FindLitShader());
            material.name = "M_" + kind;
            SetColor(material, spec.color);

            if (material.HasProperty(MetallicId))
            {
                material.SetFloat(MetallicId, spec.metallic);
            }

            if (material.HasProperty(SmoothnessId))
            {
                material.SetFloat(SmoothnessId, spec.smoothness);
            }

            if (material.HasProperty(GlossinessId))
            {
                material.SetFloat(GlossinessId, spec.smoothness);
            }

            if (spec.emissive)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                SetEmission(material, spec.color * 1.5f);
            }

            return material;
        }

        public static Spec GetSpec(MaterialKind kind)
        {
            switch (kind)
            {
                case MaterialKind.Asphalt: return new Spec(new Color(0.18f, 0.19f, 0.21f), 0f, 0.15f);
                case MaterialKind.LineWhite: return new Spec(new Color(0.95f, 0.95f, 0.95f), 0f, 0.3f);
                case MaterialKind.LineYellow: return new Spec(new Color(0.98f, 0.8f, 0.1f), 0f, 0.3f);
                case MaterialKind.Concrete: return new Spec(new Color(0.55f, 0.55f, 0.53f), 0f, 0.1f);
                case MaterialKind.Grass: return new Spec(new Color(0.25f, 0.45f, 0.18f), 0f, 0.05f);
                case MaterialKind.Cone: return new Spec(new Color(1f, 0.42f, 0.05f), 0f, 0.4f);
                case MaterialKind.CarPaint: return new Spec(new Color(0.9f, 0.9f, 0.9f), 0.4f, 0.75f);
                case MaterialKind.Glass: return new Spec(new Color(0.08f, 0.1f, 0.13f), 0.2f, 0.95f);
                case MaterialKind.Tyre: return new Spec(new Color(0.06f, 0.06f, 0.06f), 0f, 0.2f);
                case MaterialKind.Rim: return new Spec(new Color(0.62f, 0.64f, 0.67f), 0.8f, 0.7f);
                case MaterialKind.Plastic: return new Spec(new Color(0.1f, 0.1f, 0.11f), 0f, 0.35f);
                case MaterialKind.Emissive: return new Spec(Color.white, 0f, 0.5f, true);
                case MaterialKind.GarageFloor: return new Spec(new Color(0.12f, 0.13f, 0.15f), 0.2f, 0.85f);
                case MaterialKind.GarageWall: return new Spec(new Color(0.2f, 0.22f, 0.26f), 0f, 0.3f);
                case MaterialKind.Bark: return new Spec(new Color(0.35f, 0.24f, 0.15f), 0f, 0.1f);
                case MaterialKind.Foliage: return new Spec(new Color(0.18f, 0.42f, 0.16f), 0f, 0.1f);
                case MaterialKind.Building: return new Spec(new Color(0.86f, 0.8f, 0.68f), 0f, 0.15f);
                default: return new Spec(Color.magenta, 0f, 0.2f);
            }
        }

        public static void SetColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }
        }

        public static void SetEmission(Material material, Color emission)
        {
            if (material == null || !material.HasProperty(EmissionColorId))
            {
                return;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, emission);
        }
    }
}
