using System;
using System.Text;
using System.Text.RegularExpressions;

namespace UzbCarParking.Garage
{
    /// <summary>
    /// O‘yin ichidagi (virtual) davlat raqami formati.
    /// Jismoniy shaxs: "01 A 777 AA", yuridik shaxs: "01 777 AAA".
    /// Bu faqat bezak uchun; haqiqiy raqamlar bazasi bilan bog‘lanmagan.
    /// </summary>
    public static class PlateNumber
    {
        public static readonly string[] Regions =
        {
            "01", "10", "20", "25", "30", "40", "50", "60", "70", "75", "80", "85", "90", "95"
        };

        public static readonly string[] RegionNames =
        {
            "Toshkent sh.", "Toshkent vil.", "Sirdaryo", "Jizzax", "Samarqand", "Farg‘ona", "Namangan",
            "Andijon", "Qashqadaryo", "Surxondaryo", "Buxoro", "Navoiy", "Xorazm", "Qoraqalpog‘iston"
        };

        private static readonly Regex IndividualPattern = new Regex("^[A-Z][0-9]{3}[A-Z]{2}$");
        private static readonly Regex LegalPattern = new Regex("^[0-9]{3}[A-Z]{3}$");

        public static bool IsValidRegion(string region)
        {
            return !string.IsNullOrEmpty(region) && Array.IndexOf(Regions, region) >= 0;
        }

        public static int RegionIndex(string region)
        {
            int index = Array.IndexOf(Regions, region);
            return index < 0 ? 0 : index;
        }

        /// <summary>Bo‘sh joy va belgilarni olib tashlaydi, katta harfga o‘tkazadi.</summary>
        public static string Normalize(string number)
        {
            if (string.IsNullOrEmpty(number))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(number.Length);
            string upper = number.ToUpperInvariant();
            for (int i = 0; i < upper.Length; i++)
            {
                char c = upper[i];
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public static bool IsValidNumber(string number)
        {
            string normalized = Normalize(number);
            return IndividualPattern.IsMatch(normalized) || LegalPattern.IsMatch(normalized);
        }

        public static string FormatNumber(string number)
        {
            string n = Normalize(number);
            if (IndividualPattern.IsMatch(n))
            {
                return n.Substring(0, 1) + " " + n.Substring(1, 3) + " " + n.Substring(4, 2);
            }

            if (LegalPattern.IsMatch(n))
            {
                return n.Substring(0, 3) + " " + n.Substring(3, 3);
            }

            return n;
        }

        public static string Format(string region, string number)
        {
            string safeRegion = IsValidRegion(region) ? region : Regions[0];
            return safeRegion + " " + FormatNumber(number);
        }
    }
}
