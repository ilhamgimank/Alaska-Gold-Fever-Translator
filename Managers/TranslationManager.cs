// Managers/TranslationManager.cs (Fitur memuat, menambah, dan menyimpan data terjemahan)
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class TranslationManager
    {
        // Dictionary utama untuk Teks Statis
        public static Dictionary<string, string> TranslatedStrings { get; private set; }

        // Dictionary khusus untuk Pola Regex
        public static Dictionary<string, string> TranslatedRegexs { get; private set; }

        // HashSet untuk menyimpan hasil terjemahan agar dikenali dumper
        public static HashSet<string> TranslatedValues { get; private set; }

        public static string CurrentLanguage { get; set; } = "Indonesian";

        private static readonly object _lock = new object();

        public static void Initialize()
        {
            TranslatedStrings = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            TranslatedRegexs = new Dictionary<string, string>();
            TranslatedValues = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            LoadTranslations();

            Main.Logger.LogInfo("Translation Manager initialized with Regex Support.");
        }

        public static void LoadTranslations()
        {
            lock (_lock)
            {
                TranslatedStrings.Clear();
                TranslatedRegexs.Clear();
                TranslatedValues.Clear();
            }

            string languagePath = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings");

            if (Directory.Exists(languagePath))
            {
                // 1. Memuat Teks Statis
                string stringsPath = Path.Combine(languagePath, "translation_strings.json");
                if (File.Exists(stringsPath))
                {
                    ParseSimpleJson(File.ReadAllText(stringsPath, System.Text.Encoding.UTF8), TranslatedStrings);
                    SaveTranslationsToFile(); // Bersihkan duplikat case-insensitive
                }

                // 2. Memuat Teks Regex (Dukung penamaan dengan s atau tanpa s)
                string regexPath1 = Path.Combine(languagePath, "translation_regex.json");
                string regexPath2 = Path.Combine(languagePath, "translation_regexs.json");

                if (File.Exists(regexPath1)) ParseSimpleJson(File.ReadAllText(regexPath1, System.Text.Encoding.UTF8), TranslatedRegexs);
                if (File.Exists(regexPath2)) ParseSimpleJson(File.ReadAllText(regexPath2, System.Text.Encoding.UTF8), TranslatedRegexs);

                Main.Logger.LogInfo($"Loaded {TranslatedStrings.Count} static strings and {TranslatedRegexs.Count} regex patterns for language: {CurrentLanguage}.");
            }
            else
            {
                Main.Logger.LogWarning($"Localization folder for {CurrentLanguage} not found!");
            }
        }

        // Fungsi Cerdas untuk mengartikan teks. Digunakan oleh seluruh Patch in-game!
        public static bool TryTranslate(string originalText, out string translatedText)
        {
            translatedText = null;
            if (string.IsNullOrEmpty(originalText)) return false;

            lock (_lock)
            {
                // 1. Pengecekan Cepat Teks Statis
                if (TranslatedStrings.TryGetValue(originalText, out translatedText))
                {
                    return true;
                }

                // 2. Pengecekan Pola Regex
                foreach (var kvp in TranslatedRegexs)
                {
                    // Memeriksa apakah teks cocok dengan pola regex (ditambah ^ dan $ agar akurat penuh)
                    Match match = Regex.Match(originalText, "^" + kvp.Key + "$", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        try
                        {
                            // Mengumpulkan semua angka yang tertangkap ke dalam array parameter
                            object[] args = new object[match.Groups.Count - 1];
                            for (int i = 1; i < match.Groups.Count; i++)
                            {
                                args[i - 1] = match.Groups[i].Value;
                            }

                            // Memasukkan angka asli ke dalam terjemahan format (Contoh: "{0}x Barang")
                            translatedText = string.Format(kvp.Value, args);

                            // Masukkan ke memori agar Dumper tahu ini sudah diterjemahkan
                            TranslatedValues.Add(translatedText);
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Main.Logger.LogError($"[Regex Format Error] Pattern: {kvp.Key} | Error: {ex.Message}");
                        }
                    }
                }
            }

            return false;
        }

        private static void ParseSimpleJson(string json, Dictionary<string, string> targetDictionary)
        {
            string pattern = "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            MatchCollection matches = Regex.Matches(json, pattern);

            lock (_lock)
            {
                foreach (Match match in matches)
                {
                    string originalText = UnescapeFromJson(match.Groups[1].Value);
                    string translatedText = UnescapeFromJson(match.Groups[2].Value);

                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        targetDictionary[originalText] = translatedText;
                        TranslatedValues.Add(translatedText);
                    }
                }
            }
        }

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

        // [FITUR BARU] Fungsi untuk menyimpan hasil format Regex otomatis dari Auto Translator
        public static void AddAndSaveRegexTranslation(string regexKey, string translatedFormat)
        {
            if (string.IsNullOrEmpty(regexKey) || string.IsNullOrEmpty(translatedFormat)) return;

            lock (_lock)
            {
                TranslatedRegexs[regexKey] = translatedFormat;
            }
            SaveRegexTranslationsToFile();
        }

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
                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TranslationManager] Error saving JSON: {ex.Message}");
            }
        }

        // [FITUR BARU] Fungsi menulis ke translation_regexs.json
        private static void SaveRegexTranslationsToFile()
        {
            try
            {
                string languagePath = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings");
                string filePath = Path.Combine(languagePath, "translation_regexs.json");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");

                lock (_lock)
                {
                    int count = 0;
                    foreach (var kvp in TranslatedRegexs)
                    {
                        string escapedKey = EscapeForJson(kvp.Key);
                        string escapedValue = EscapeForJson(kvp.Value);

                        sb.Append($"  \"{escapedKey}\": \"{escapedValue}\"");

                        if (count < TranslatedRegexs.Count - 1) sb.AppendLine(",");
                        else sb.AppendLine();

                        count++;
                    }
                }

                sb.AppendLine("}");
                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TranslationManager] Error saving Regex JSON: {ex.Message}");
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