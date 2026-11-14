using System;
using UnityEngine;

namespace UzbCarParking.Core
{
    /// <summary>
    /// Modullar orasidagi yengil global hodisalar (masalan, ekranga qisqa xabar chiqarish).
    /// Obunachilar OnDestroy'da albatta obunani bekor qilishi kerak.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> ToastRequested;

        public static void Toast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (ToastRequested != null)
            {
                ToastRequested.Invoke(message);
            }
            else
            {
                Debug.Log("[UZB] " + message);
            }
        }
    }

    public static class ColorUtil
    {
        public static Color FromHex(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }

            return fallback;
        }

        public static string ToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }

    public static class GameFonts
    {
        private static Font defaultFont;

        /// <summary>Unity 6 ichki shrifti. TextMeshPro keyingi bosqichda qo‘shiladi.</summary>
        public static Font Default
        {
            get
            {
                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return defaultFont;
            }
        }
    }
}
