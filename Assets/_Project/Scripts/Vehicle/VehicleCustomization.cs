using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.Garage;
using UzbCarParking.Persistence;

namespace UzbCarParking.Vehicle
{
    /// <summary>
    /// Kuzov rangi, disk rangi va davlat raqamini mashinaga qo‘llaydi.
    /// Rang uchun bitta umumiy material nusxasi yaratiladi (batching buzilmasligi uchun).
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleCustomization : MonoBehaviour
    {
        public Renderer[] paintRenderers = new Renderer[0];
        public Renderer[] rimRenderers = new Renderer[0];
        public TextMesh[] plateTexts = new TextMesh[0];

        private Material paintInstance;
        private Material rimInstance;

        public Color PaintColor { get; private set; }
        public Color RimColor { get; private set; }

        public void Apply(VehicleCustomizationData data)
        {
            if (data == null)
            {
                return;
            }

            SetPaint(ColorUtil.FromHex(data.paintColorHex, new Color(0.9f, 0.9f, 0.9f)));
            SetRims(ColorUtil.FromHex(data.rimColorHex, new Color(0.62f, 0.64f, 0.67f)));
            SetPlate(data.plateRegion, data.plateNumber);
        }

        public void SetPaint(Color color)
        {
            PaintColor = color;
            EnsureInstance(ref paintInstance, paintRenderers);
            RuntimeMaterials.SetColor(paintInstance, color);
        }

        public void SetRims(Color color)
        {
            RimColor = color;
            EnsureInstance(ref rimInstance, rimRenderers);
            RuntimeMaterials.SetColor(rimInstance, color);
        }

        public void SetPlate(string region, string number)
        {
            if (plateTexts == null)
            {
                return;
            }

            string text = PlateNumber.Format(region, number);
            for (int i = 0; i < plateTexts.Length; i++)
            {
                if (plateTexts[i] != null)
                {
                    plateTexts[i].text = text;
                }
            }
        }

        /// <summary>
        /// Birinchi renderer'ning materialidan nusxa oladi va shu materialni ishlatgan barcha
        /// slotlarni nusxa bilan almashtiradi.
        /// </summary>
        private static void EnsureInstance(ref Material instance, Renderer[] renderers)
        {
            if (instance != null || renderers == null || renderers.Length == 0)
            {
                return;
            }

            Material source = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].sharedMaterial != null)
                {
                    source = renderers[i].sharedMaterial;
                    break;
                }
            }

            if (source == null)
            {
                return;
            }

            instance = new Material(source);
            instance.name = source.name + " (Custom)";

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Material[] materials = renderers[i].sharedMaterials;
                bool changed = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == source)
                    {
                        materials[m] = instance;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderers[i].sharedMaterials = materials;
                }
            }
        }

        private void OnDestroy()
        {
            if (paintInstance != null) Destroy(paintInstance);
            if (rimInstance != null) Destroy(rimInstance);
        }
    }
}
