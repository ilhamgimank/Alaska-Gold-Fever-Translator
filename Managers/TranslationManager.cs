using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class TranslationManager
    {
        public static Dictionary<string, string> TranslatedStrings { get; private set; }
        public static Dictionary<string, string> TranslatedRegexs { get; private set; }
        public static HashSet<string> TranslatedValues { get; private set; }
        public static HashSet<string> TranslatedRegexValuesAsPatterns { get; private set; }

        public static string CurrentLanguage { get; set; } = "Indonesian";
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            TranslatedStrings = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            TranslatedRegexs = new Dictionary<string, string>();
            TranslatedValues = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            TranslatedRegexValuesAsPatterns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            LoadTranslations();

            Main.Logger.LogInfo("Translation Manager initialized (Clean Stable Version).");
        }

        public static void LoadTranslations()
        {
            lock (_lock)
            {
                TranslatedStrings.Clear();
                TranslatedRegexs.Clear();
                TranslatedValues.Clear();
                TranslatedRegexValuesAsPatterns.Clear();
            }

            string languagePath = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings");
            if (Directory.Exists(languagePath))
            {
                string staticPath = Path.Combine(languagePath, "translation_strings.json");
                if (File.Exists(staticPath))
                {
                    ParseSimpleJson(File.ReadAllText(staticPath, System.Text.Encoding.UTF8), TranslatedStrings);

                    lock (_lock)
                    {
                        foreach (var kvp in TranslatedStrings)
                        {
                            TranslatedValues.Add(kvp.Value);
                        }
                    }
                }

                string regexPath1 = Path.Combine(languagePath, "translation_regexs.json");
                string regexPath2 = Path.Combine(languagePath, "translation_regex.json");

                if (File.Exists(regexPath1)) ParseSimpleJson(File.ReadAllText(regexPath1, System.Text.Encoding.UTF8), TranslatedRegexs);
                if (File.Exists(regexPath2)) ParseSimpleJson(File.ReadAllText(regexPath2, System.Text.Encoding.UTF8), TranslatedRegexs);

                lock (_lock)
                {
                    foreach (var kvp in TranslatedRegexs)
                    {
                        RegisterRegexValuePattern(kvp.Value);
                    }
                }

                SaveTranslationsToFile();
                Main.Logger.LogInfo($"Loaded {TranslatedStrings.Count} static strings and {TranslatedRegexs.Count} regex patterns.");
            }
        }

        public static bool TryGetTranslation(string originalText, out string translatedText)
        {
            return TranslatedStrings.TryGetValue(originalText, out translatedText);
        }

        public static bool TryGetRegexTranslation(string originalText, out string translatedText)
        {
            translatedText = null;
            if (string.IsNullOrEmpty(originalText)) return false;

            lock (_lock)
            {
                foreach (var kvp in TranslatedRegexs)
                {
                    var match = Regex.Match(originalText, "^" + kvp.Key + "$");
                    if (match.Success)
                    {
                        List<string> args = new List<string>();
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            args.Add(match.Groups[i].Value);
                        }

                        try
                        {
                            translatedText = string.Format(kvp.Value, args.ToArray());
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        public static void AddAndSaveTranslation(string original, string translated)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(translated)) return;

            lock (_lock)
            {
                TranslatedStrings[original] = translated;
                TranslatedValues.Add(translated);
            }
            SaveTranslationsToFile();
        }

        public static void AddAndSaveRegexTranslation(string regexKey, string translatedFormat)
        {
            if (string.IsNullOrEmpty(regexKey) || string.IsNullOrEmpty(translatedFormat)) return;

            lock (_lock)
            {
                TranslatedRegexs[regexKey] = translatedFormat;
                RegisterRegexValuePattern(translatedFormat);
            }
            SaveRegexTranslationsToFile();
        }

        private static void RegisterRegexValuePattern(string translatedFormat)
        {
            if (string.IsNullOrEmpty(translatedFormat)) return;
            string escaped = EscapeForRegexPattern(translatedFormat);
            string pattern = Regex.Replace(escaped, @"\\\{\d+\\\}", @"(\d+)");

            lock (_lock)
            {
                TranslatedRegexValuesAsPatterns.Add(pattern);
            }
        }

        private static string EscapeForRegexPattern(string text)
        {
            string[] specialChars = { "\\", "^", "$", ".", "|", "?", "*", "+", "(", ")", "[", "]", "{", "}" };
            string safeText = text;
            foreach (var c in specialChars)
            {
                safeText = safeText.Replace(c, "\\" + c);
            }
            return safeText;
        }

        private static void SaveTranslationsToFile()
        {
            lock (_lock)
            {
                string path = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings", "translation_strings.json");
                WriteDictToJson(path, TranslatedStrings);
            }
        }

        private static void SaveRegexTranslationsToFile()
        {
            lock (_lock)
            {
                string path = Path.Combine(FileManager.LocalizationPath, CurrentLanguage, "Strings", "translation_regexs.json");
                WriteDictToJson(path, TranslatedRegexs);
            }
        }

        private static void WriteDictToJson(string path, Dictionary<string, string> dict)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("{");
                    int count = 0;
                    foreach (var kvp in dict)
                    {
                        count++;
                        writer.Write($"  \"{EscapeForJson(kvp.Key)}\": \"{EscapeForJson(kvp.Value)}\"");
                        if (count < dict.Count) writer.WriteLine(",");
                        else writer.WriteLine();
                    }
                    writer.WriteLine("}");
                }
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError("Error saving JSON: " + ex.Message);
            }
        }

        public static void ParseSimpleJson(string json, Dictionary<string, string> dict)
        {
            var matches = Regex.Matches(json, "\"([^\"]*)\"\\s*:\\s*\"([^\"]*)\"");
            foreach (Match m in matches)
            {
                string key = UnescapeJson(m.Groups[1].Value);
                string val = UnescapeJson(m.Groups[2].Value);
                dict[key] = val;
            }
        }

        public static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}