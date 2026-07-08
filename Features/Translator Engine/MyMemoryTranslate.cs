// Features/Translator Engine/MyMemoryTranslate.cs (Mesin penerjemah alternatif via MyMemory API)
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features.TranslatorEngine
{
    public static class MyMemoryTranslate
    {
        public static async Task<string> TranslateAsync(string sourceText, string sourceLang = "en", string targetLang = "id")
        {
            try
            {
                // Mengamankan karakter untuk URL
                string safeText = Uri.EscapeDataString(sourceText);

                // Endpoint gratis MyMemory
                string url = $"https://api.mymemory.translated.net/get?q={safeText}&langpair={sourceLang}|{targetLang}";

                WebRequest request = WebRequest.Create(url);
                request.Timeout = 5000;

                using (WebResponse response = await request.GetResponseAsync())
                using (Stream dataStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    string jsonResponse = await reader.ReadToEndAsync();
                    return ParseMyMemoryResponse(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                Main.Logger.LogError($"[MyMemoryTranslate] Error translating '{sourceText}': {ex.Message}");
                return null;
            }
        }

        private static string ParseMyMemoryResponse(string json)
        {
            try
            {
                // Mencari target {"translatedText":"Hasil Terjemahan"}
                var match = Regex.Match(json, @"\""translatedText\""\s*:\s*\""(.*?)\""");

                if (match.Success)
                {
                    string translated = match.Groups[1].Value;

                    // Mengembalikan karakter escape (seperti \n, \t, dan unicode)
                    translated = Regex.Unescape(translated);

                    return translated.Trim();
                }
            }
            catch (Exception ex)
            {
                Main.Logger.LogError($"[MyMemoryTranslate] Parse Error: {ex.Message}");
            }

            return null;
        }
    }
}