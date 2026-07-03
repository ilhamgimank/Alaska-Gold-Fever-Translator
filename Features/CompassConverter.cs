// Features/CompassConverter.cs (Fitur pengubah arah mata angin kompas khusus)
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CompassConverter
    {
        // [PERBAIKAN KRUSIAL] Menggunakan OrdinalIgnoreCase agar huruf kecil seperti "w", "sw", "nw" ikut terdeteksi!
        private static readonly Dictionary<string, string> CompassDirections = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
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

        // [FITUR BARU] Regex untuk membaca arah kompas yang dibungkus oleh Rich Text Tag (misal: <color=red>W</color> atau <sprite=1>NW)
        private static readonly Regex CompassRegex = new Regex(@"^((?:<[^>]+>|\s)*)([a-zA-Z]{1,2})((?:<[^>]+>|\s)*)$", RegexOptions.Compiled);

        public static string Convert(string text, Component uiComponent = null)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // [REVISI] Menggunakan Regex agar Tag HTML/TMP tidak dihitung sebagai panjang huruf!
            Match match = CompassRegex.Match(text);
            if (!match.Success) return text;

            string prefix = match.Groups[1].Value;
            string coreText = match.Groups[2].Value; // Ini pasti hanya 1 atau 2 huruf murni (misal: W, NW, sw)
            string suffix = match.Groups[3].Value;

            // Cek ke dalam dictionary (Sekarang kebal huruf besar/kecil!)
            if (CompassDirections.TryGetValue(coreText, out string translatedDir))
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

                // [SMART CASING] Menyesuaikan huruf besar/kecil sesuai teks aslinya (sw -> bd, W -> B)
                if (coreText == coreText.ToLower())
                {
                    translatedDir = translatedDir.ToLower();
                }

                // Menggabungkan kembali Prefix (Tag/Spasi) + Teks Terjemahan + Suffix (Tag/Spasi)
                return prefix + translatedDir + suffix;
            }

            return text;
        }
    }
}