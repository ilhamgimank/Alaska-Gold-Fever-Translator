// Features/CompassConverter.cs (Fitur pengubah arah mata angin kompas khusus)
using UnityEngine;
using System.Collections.Generic;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CompassConverter
    {
        private static readonly Dictionary<string, string> CompassDirections = new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            { "N", "U" },
            { "NE", "TL" },
            { "E", "T" },
            { "SE", "TG" },
            { "S", "S" },
            { "SW", "BD" },
            { "W", "B" },
            { "NW", "BL" }
        };

        public static string Convert(string text, Component uiComponent = null)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Menghapus spasi sementara untuk pengecekan (misal " W ")
            string cleanText = text.Trim();

            // Arah mata angin maksimal hanya 2 huruf, abaikan jika lebih panjang
            if (cleanText.Length > 2) return text;

            if (CompassDirections.TryGetValue(cleanText, out string translatedDir))
            {
                // [SMART FILTER] Mencegah tombol keyboard (W, A, S, D, E) ikut diterjemahkan!
                if (uiComponent != null)
                {
                    string objName = uiComponent.gameObject.name.ToLower();
                    string parentName = uiComponent.transform.parent != null ? uiComponent.transform.parent.name.ToLower() : "";

                    // Jika nama objek atau parent-nya mengandung unsur "tombol", "key", atau "prompt", batalkan konversi!
                    if (objName.Contains("key") || objName.Contains("btn") || objName.Contains("button") || objName.Contains("prompt") ||
                        parentName.Contains("key") || parentName.Contains("btn") || parentName.Contains("button") || parentName.Contains("prompt"))
                    {
                        return text;
                    }
                }

                // Mengembalikan hasil terjemahan namun tetap menjaga spasi aslinya (jika ada)
                return text.Replace(cleanText, translatedDir);
            }

            return text;
        }
    }
}