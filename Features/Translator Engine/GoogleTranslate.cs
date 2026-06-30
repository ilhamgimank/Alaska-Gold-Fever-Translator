// Features/Translator Engine/GoogleTranslate.cs (Mesin penerjemah via Google API Endpoint)
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features.TranslatorEngine
{
    public static class GoogleTranslate
    {
        // Fungsi asinkron untuk menerjemahkan teks tanpa membuat game lag/freeze
        public static async Task<string> TranslateAsync(string sourceText, string sourceLang = "en", string targetLang = "id")
        {
            try
            {
                // Mengamankan karakter agar aman masuk ke URL (Contoh: Spasi menjadi %20)
                string safeText = Uri.EscapeDataString(sourceText);

                // Endpoint gratis Google Translate (Digunakan untuk Web Scraping / Ekstensi Chrome)
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={safeText}";

                WebRequest request = WebRequest.Create(url);
                request.Timeout = 5000; // Timeout 5 detik agar tidak menggantung selamanya

                // Melakukan request secara asinkron
                using (WebResponse response = await request.GetResponseAsync())
                using (Stream dataStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    string jsonResponse = await reader.ReadToEndAsync();
                    return ParseGoogleResponse(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                Main.Logger.LogError($"[GoogleTranslate] Error translating '{sourceText}': {ex.Message}");
                return null;
            }
        }

        // Fungsi untuk mengambil teks terjemahan dan membersihkan bug format JSON
        private static string ParseGoogleResponse(string json)
        {
            string translated = "";

            try
            {
                // Memotong respon JSON hanya pada bagian array pertama (tempat terjemahan berada)
                // Ini mencegah Regex menangkap ID Token / Hash metadata yang ada di akhir JSON Google!
                int stopIndex = json.IndexOf("],null,");
                if (stopIndex == -1) stopIndex = json.IndexOf("],\""); // Fallback jika respon berbeda

                string targetBlock = stopIndex > -1 ? json.Substring(0, stopIndex) : json;

                // Menangkap teks menggunakan Regex
                var matches = Regex.Matches(targetBlock, @"\[\""(.*?)\"",""(.*?)\""");

                if (matches.Count > 0)
                {
                    foreach (Match m in matches)
                    {
                        translated += m.Groups[1].Value;
                    }

                    // Mengubah kode unicode (seperti \u003cbr\u003e) kembali menjadi simbol asli (<br>)
                    // serta otomatis menangani escape character standar lainnya (\n, \t).
                    translated = Regex.Unescape(translated);

                    // [PERBAIKAN BUG v0.1.5] Penghapusan Absolut Hash Artifact API.
                    // Menggunakan metode Replace langsung agar huruf terakhir dari terjemahan 
                    // (seperti huruf 'a' pada "berbahaya") tidak ikut terpotong oleh Regex Hex.
                    translated = translated.Replace("8197e9010ff5cd59d89a62790d9829cf", "");

                    // Membersihkan sisa spasi kosong di awal/akhir jika ada
                    translated = translated.Trim();
                }
            }
            catch (Exception ex)
            {
                Main.Logger.LogError($"[GoogleTranslate] Parse Error: {ex.Message}");
            }

            return translated;
        }
    }
}