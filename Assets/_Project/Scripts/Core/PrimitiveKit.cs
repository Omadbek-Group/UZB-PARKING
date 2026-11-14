using UnityEngine;

namespace UzbCarParking.Core
{
    /// <summary>
    /// Vaqtinchalik (placeholder) geometriya yaratish uchun yordamchi metodlar.
    /// Haqiqiy 3D modellar tayyor bo‘lgach, shu primitive'lar prefablar bilan almashtiriladi.
    /// </summary>
    public static class PrimitiveKit
    {
        public static Transform Child(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Kollayderi bor primitive (to‘siq, yer, devor).</summary>
        public static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, Quaternion? localRotation = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }

        /// <summary>Faqat ko‘rinish uchun primitive (kollayder olib tashlanadi).</summary>
        public static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, Quaternion? localRotation = null)
        {
            GameObject go = Solid(type, name, parent, localPosition, localScale, material, localRotation);
            RemoveCollider(go);
            return go;
        }

        public static void RemoveCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        /// <summary>3D matn (belgi, davlat raqami, yerdagi yozuv).</summary>
        public static TextMesh Label(string text, Transform parent, Vector3 localPosition, Quaternion localRotation,
            float characterSize, Color color, int fontSize = 64)
        {
            GameObject go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;

            TextMesh mesh = go.AddComponent<TextMesh>();
            Font font = GameFonts.Default;
            mesh.font = font;
            mesh.text = text;
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = font.material;
            }

            return mesh;
        }
    }
}
