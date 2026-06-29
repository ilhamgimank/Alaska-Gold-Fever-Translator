// Managers/TranslationManager.cs (Fitur memuat, menambah, dan menyimpan data terjemahan) (Update: Menambah Hashset TranslatedValues)
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class TranslationManager
    {
        // Dictionary utama untuk Teks Inggris -> Indonesia
        public static Dictionary<string, string> TranslatedStrings { get; private set; }

        // HashSet khusus untuk menyimpan hasil bahasa Indonesia (Agar dumper tahu ini sudah diterjemahkan)
        public static HashSet<string> TranslatedValues { get; private set; }

        public static string CurrentLanguage { get; set; } = "Indonesian";

        // Objek pengunci untuk mengamankan proses modifikasi dictionary dari thread AutoTranslator
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            TranslatedStrings = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            TranslatedValues = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            LoadTranslations();

            Main.Logger.LogInfo("Translation Manager initialized (Case-Insensitive mode active).");
        }

        public static void LoadTranslations()
        {
            lock (_lock)
            {
                TranslatedStrings.Clear();
                TranslatedValues.Clear();
            }

            string languagePath = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings");

            if (Directory.Exists(languagePath))
            {
                string[] jsonFiles = Directory.GetFiles(languagePath, "*.json");

                foreach (string file in jsonFiles)
                {
                    string jsonContent = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    ParseSimpleJson(jsonContent);
                }

                Main.Logger.LogInfo($"Loaded {TranslatedStrings.Count} translated strings for language: {CurrentLanguage}.");

                // [FITUR BARU] Otomatis menyimpan ulang untuk membersihkan dan menghapus teks duplikat di file JSON
                SaveTranslationsToFile();
            }
            else
            {
                Main.Logger.LogWarning($"Localization folder for {CurrentLanguage} not found!");
            }
        }

        private static void ParseSimpleJson(string json)
        {
            string pattern = "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            MatchCollection matches = Regex.Matches(json, pattern);

            lock (_lock)
            {
                int duplicateCount = 0;
                foreach (Match match in matches)
                {
                    string originalText = UnescapeFromJson(match.Groups[1].Value);
                    string translatedText = UnescapeFromJson(match.Groups[2].Value);

                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        // Hitung jika teks ini duplikat (Case-Insensitive)
                        if (TranslatedStrings.ContainsKey(originalText)) duplicateCount++;

                        TranslatedStrings[originalText] = translatedText;

                        // Memasukkan hasil terjemahan ke HashSet agar dikenali sebagai "Bukan teks asli"
                        TranslatedValues.Add(translatedText);
                    }
                }

                if (duplicateCount > 0)
                {
                    Main.Logger.LogInfo($"[TranslationManager] Found and removed {duplicateCount} duplicate entries. JSON file has been cleaned.");
                }
            }
        }

        // Method yang dipanggil oleh AutoTranslator untuk memasukkan data baru
        public static void AddAndSaveTranslation(string originalText, string translatedText)
        {
            if (string.IsNullOrEmpty(originalText) || string.IsNullOrEmpty(translatedText)) return;

            lock (_lock)
            {
                TranslatedStrings[originalText] = translatedText;
                TranslatedValues.Add(translatedText);
            }

            SaveTranslationsToFile();
        }

        // Menyimpan data terjemahan ke dalam file translation_strings.json
        private static void SaveTranslationsToFile()
        {
            try
            {
                string languagePath = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings");
                string filePath = Path.Combine(languagePath, "translation_strings.json");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");

                lock (_lock)
                {
                    int count = 0;
                    foreach (var kvp in TranslatedStrings)
                    {
                        string escapedKey = EscapeForJson(kvp.Key);
                        string escapedValue = EscapeForJson(kvp.Value);

                        sb.Append($"  \"{escapedKey}\": \"{escapedValue}\"");

                        if (count < TranslatedStrings.Count - 1) sb.AppendLine(",");
                        else sb.AppendLine();

                        count++;
                    }
                }

                sb.AppendLine("}");

                // Wajib menggunakan UTF8 agar karakter Indonesia/Asia tidak rusak
                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TranslationManager] Error saving JSON: {ex.Message}");
            }
        }

        private static string UnescapeFromJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\\", "\\");
        }

        private static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}