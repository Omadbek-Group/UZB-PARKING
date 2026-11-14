using System;
using System.Collections.Generic;
using UnityEngine;

namespace UzbCarParking.Core
{
    public enum MaterialKind
    {
        Asphalt = 0,
        LineWhite = 1,
        LineYellow = 2,
        Concrete = 3,
        Grass = 4,
        Cone = 5,
        CarPaint = 6,
        Glass = 7,
        Tyre = 8,
        Rim = 9,
        Plastic = 10,
        Emissive = 11,
        GarageFloor = 12,
        GarageWall = 13,
        Bark = 14,
        Foliage = 15,
        Building = 16
    }

    /// <summary>
    /// Placeholder materiallar to‘plami. Asset sifatida saqlanadi, shunda URP shaderlari
    /// Android build'ga kiritiladi (runtime'da Shader.Find'ga tayanilmaydi).
    /// "UZB Car Parking ▸ 1. MVP loyihani sozlash" menyusi uni avtomatik yaratadi.
    /// </summary>
    [CreateAssetMenu(menuName = "UZB Car Parking/Material Library", fileName = "MaterialLibrary")]
    public class MaterialLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public MaterialKind kind;
            public Material material;
        }

        public List<Entry> entries = new List<Entry>();

        public Material Get(MaterialKind kind)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].kind == kind)
                {
                    return entries[i].material;
                }
            }

            return null;
        }

        public void Set(MaterialKind kind, Material material)
        {
            Entry entry = new Entry { kind = kind, material = material };
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].kind == kind)
                {
                    entries[i] = entry;
                    return;
                }
            }

            entries.Add(entry);
        }
    }
}
