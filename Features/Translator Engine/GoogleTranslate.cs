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

        // Fungsi untuk mengambil teks terjemahan dari format JSON aneh bawaan Google
        private static string ParseGoogleResponse(string json)
        {
            string translated = "";

            // Format respon Google: [[["Teks Indo","Text Eng",null,null,1],["Teks Indo 2","Text Eng 2",null,null,1]],null,"en"]
            // Kita gunakan Regex untuk mengambil semua teks terjemahan yang ada di indeks pertama array
            var matches = Regex.Matches(json, @"\[\""(.*?)\"",""(.*?)\""");

            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    translated += m.Groups[1].Value;
                }

                // Mengembalikan karakter escape ke bentuk normal
                translated = translated.Replace("\\n", "\n")
                                       .Replace("\\\"", "\"")
                                       .Replace("\\r", "\r")
                                       .Replace("\\t", "\t")
                                       .Replace("\\\\", "\\");
            }

            return translated;
        }
    }
}