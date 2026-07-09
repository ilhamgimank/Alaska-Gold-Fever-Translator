// Features/CompassConverter.cs (Fitur pengubah arah mata angin kompas khusus)
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CompassConverter
    {
        private static readonly Dictionary<string, string> CompassDirections = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "N", "U" }, { "NE", "TL" }, { "E", "T" }, { "SE", "TG" },
            { "S", "S" }, { "SW", "BD" }, { "W", "B" }, { "NW", "BL" }
        };

        public static string Convert(string text, Component uiComponent = null)
        {
            // Abaikan jika kosong atau terlalu panjang (teks kompas selalu sangat pendek)
            if (string.IsNullOrEmpty(text) || text.Length > 15) return text;

            // Bersihkan teks dari Tag HTML / Rich Text untuk proses analisa
            string cleanText = Regex.Replace(text, @"<[^>]+>", "").Trim();

            // Ekstrak HANYA huruf alfanya saja (misal: "315° NW" akan terbaca murni "NW")
            string justLetters = Regex.Replace(cleanText, @"[^a-zA-Z]", "");

            if (CompassDirections.TryGetValue(justLetters, out string translatedDir))
            {
                // [SMART FILTER] Mencegah tombol keyboard "Interact" (W, A, S, D, E) ikut diterjemahkan!
                if (uiComponent != null)
                {
                    string objName = uiComponent.gameObject.name.ToLower();
                    string parentName = uiComponent.transform.parent != null ? uiComponent.transform.parent.name.ToLower() : "";

                    if (objName.Contains("key") || objName.Contains("btn") || objName.Contains("button") || objName.Contains("prompt") ||
                        parentName.Contains("key") || parentName.Contains("btn") || parentName.Contains("button") || parentName.Contains("prompt"))
                    {
                        return text;
                    }
                }

                // [REPLACE AKURAT] Menimpa langsung hurufnya tanpa merusak struktur Tag Color / Simbol Derajat
                int idx = text.IndexOf(justLetters, System.StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    // Pastikan kapitalisasinya mengikuti bawaan asli (sw -> bd, NW -> BL)
                    string actualFound = text.Substring(idx, justLetters.Length);
                    string replacement = (actualFound == actualFound.ToLower()) ? translatedDir.ToLower() : translatedDir;
                    return text.Remove(idx, justLetters.Length).Insert(idx, replacement);
                }
            }

            return text;
        }
    }
}