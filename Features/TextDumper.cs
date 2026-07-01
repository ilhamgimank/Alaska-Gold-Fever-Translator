// Features/TextDumper.cs (Fitur dumper teks utama dengan Regex Auto-Formatter & Debouncer)
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AlaskaGoldFeverTranslator.Managers;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class TextDumper
    {
        public static string UntranslatedStringsPath { get; private set; }
        public static string UntranslatedRegexPath { get; private set; }

#pragma warning disable
        private static HashSet<string> _dumpedStrings = new HashSet<string>();
        private static HashSet<string> _dumpedRegexs = new HashSet<string>();

        private static readonly object _lock = new object();

        // Penanda apakah ada penyimpanan yang sedang mengantre
        private static bool _savePending = false;

        public static void Initialize()
        {
            UntranslatedStringsPath = Path.Combine(FileManager.DumpsPath, "untranslation_strings.json");
            UntranslatedRegexPath = Path.Combine(FileManager.DumpsPath, "untranslation_regexs.json");

            // Memuat riwayat dump lama agar tidak terhapus saat disave ulang
            LoadExistingDumps();

            CreateJsonFileIfNotExists(UntranslatedStringsPath, "{\n}");
            CreateJsonFileIfNotExists(UntranslatedRegexPath, "{\n}");

            Main.Logger.LogInfo("Text Dumper initialized. Old dumps loaded and preserved safely.");
        }

        private static void CreateJsonFileIfNotExists(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
                Main.Logger.LogInfo($"Created dump file: {Path.GetFileName(path)}");
            }
        }

        // Method untuk memuat file JSON lama kembali ke dalam memori HashSet
        private static void LoadExistingDumps()
        {
            if (File.Exists(UntranslatedStringsPath))
            {
                ParseJsonToHashSet(File.ReadAllText(UntranslatedStringsPath, System.Text.Encoding.UTF8), _dumpedStrings);
            }
            if (File.Exists(UntranslatedRegexPath))
            {
                ParseJsonToHashSet(File.ReadAllText(UntranslatedRegexPath, System.Text.Encoding.UTF8), _dumpedRegexs);
            }
        }

        private static void ParseJsonToHashSet(string json, HashSet<string> targetSet)
        {
            // Regex sederhana untuk menangkap Keys pada JSON
            string pattern = "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*:";
            MatchCollection matches = Regex.Matches(json, pattern);

            lock (_lock)
            {
                foreach (Match match in matches)
                {
                    string key = UnescapeFromJson(match.Groups[1].Value);
                    if (!string.IsNullOrEmpty(key)) targetSet.Add(key);
                }
            }
        }

        private static string UnescapeFromJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\\", "\\");
        }

        private static bool IsSpamText(string text, string uiType)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (text.Contains("Lorem ipsum") || text.Contains("Lorem Ipsum") || text.StartsWith("Lorem")) return true;

            // [PERBAIKAN SENSITIVITAS] Jangan langsung memblokir karakter "/" jika ada huruf alfabet di dalam kalimat!
            // Ini agar teks misi seperti "(0/1)" tetap bisa lolos dumper.
            if (text.Contains("/") && !Regex.IsMatch(text, @"[a-zA-Z]")) return true;

            // [ANTI-BOUNCING MATA UANG] Mencegah hasil konversi uang (Rp) dideteksi sebagai bahasa Inggris baru!
            if (text.Contains("Rp.") && Regex.IsMatch(text, @"Rp\.\s*\d+")) return true;

            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^Slot\s+\d+$")) return true;
            if (text.Length <= 1 && char.IsDigit(text[0])) return true;

            string[] technicalKeywords = new string[] {
                "ScreenSpace", "RayTracing", "Raytraced", "Volumetric", "Texcoord", "ShadowMask",
                "ambientOcclusion", "perceptualRoughness", "Tessellation", "ContactShadows",
                "DepthPyramid", "ColorPyramid", "LightCluster", "MotionVectors", "WorldSpace",
                "NanTracker", "ColorLog", "DepthOfField", "Overdraw", "VertexDensity", "STP",
                "VirtualTexture", "LensFlare", "ComputeThickness", "HighQualityLines", "DiffuseColor",
                "SpecularColor", "BakeDiffuse", "BakeShadow", "materialFeatures", "diffuseColor",
                "fresnel0", "fresnel90", "specularOcclusion", "coatMask", "diffusionProfileIndex",
                "subsurfaceMask", "transmittance", "tangentWS", "bitangentWS", "roughnessT", "roughnessB",
                "anisotropy", "iridescence", "coatRoughness", "Geometric Normal", "absorptionCoefficient",
                "transmittanceMask", "DeferredMaterials", "VertexTangent", "VertexBitangent", "VertexNormal",
                "VertexColor", "VertexDisplacement", "DepthOffset", "Lightmap", "Instancing", "TAAMotion",
                "No Visible Camera", "ContactShadowsFade", "PreRefractionColorPyramid", "RayTracedSubSurface"
            };

            foreach (var word in technicalKeywords)
            {
                if (text.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (!text.Contains(" ") && text.Length > 8)
            {
                int upperCount = 0;
                for (int i = 1; i < text.Length; i++)
                {
                    if (char.IsUpper(text[i])) upperCount++;
                }
                if (upperCount > 0) return true;
            }

            return false;
        }

        private static string EscapeForRegex(string text)
        {
            string[] specialChars = { "\\", "^", "$", ".", "|", "?", "*", "+", "(", ")", "[", "]", "{", "}" };
            string safeText = text;
            foreach (var c in specialChars)
            {
                safeText = safeText.Replace(c, "\\" + c);
            }
            return safeText;
        }

        public static void DumpString(string text, string uiType, bool isRegex = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (IsSpamText(text, uiType)) return;

            // [ANTI-BOUNCING KRUSIAL] Mencegah teks yang sudah berwujud terjemahan Indonesia masuk ke Dumper!
            if (TranslationManager.TranslatedStrings.ContainsKey(text)) return;
            if (TranslationManager.TranslatedValues.Contains(text)) return;

            bool hasLetter = Regex.IsMatch(text, @"[a-zA-Z]");
            bool hasNumber = Regex.IsMatch(text, @"\d+");

            // Membuang teks yang HANYA berisi angka murni (tanpa huruf) seperti "100" atau "$0.00"
            if (hasNumber && !hasLetter) return;

            // Logika Dumper Regex
            if (hasNumber && hasLetter)
            {
                string safePattern = EscapeForRegex(text);
                string regexKey = Regex.Replace(safePattern, @"\d+", @"(\d+)");

                if (regexKey != safePattern)
                {
                    if (TranslationManager.TranslatedRegexs.ContainsKey(regexKey)) return;

                    // [ANTI-BOUNCING REGEX] Cegah pola dinamis bahasa Indonesia terekam ulang menjadi Spam!
                    if (TranslationManager.TranslatedRegexValuesAsPatterns.Contains(regexKey)) return;

                    bool isNewRegex = false;
                    lock (_lock)
                    {
                        if (!_dumpedRegexs.Contains(regexKey))
                        {
                            _dumpedRegexs.Add(regexKey);
                            isNewRegex = true;
                        }
                    }

                    if (isNewRegex)
                    {
                        Main.Logger.LogInfo($"[{uiType}][New Auto-Regex] \"{regexKey}\"");
                        RequestSave();

                        // Mengirim teks aslinya ke Auto Translator beserta bendera penanda Regex
                        AutoTranslator.AddToQueue(text, true, regexKey);
                    }
                    return;
                }
            }

            // Logika Dumper Statis
            bool isNewStatic = false;
            lock (_lock)
            {
                if (!_dumpedStrings.Contains(text))
                {
                    _dumpedStrings.Add(text);
                    isNewStatic = true;
                }
            }

            if (isNewStatic)
            {
                Main.Logger.LogInfo($"[{uiType}][New Static Text Dumped] \"{text}\"");
                RequestSave();
                AutoTranslator.AddToQueue(text);
            }
        }

        // Debouncer: Mengantrekan proses penyimpanan agar Harddisk tidak dipaksa kerja keras
        private static void RequestSave()
        {
            if (_savePending) return;
            _savePending = true;

            Task.Run(async () =>
            {
                await Task.Delay(3000); // Menunggu 3 detik penuh

                SaveDataToFile(UntranslatedStringsPath, _dumpedStrings, false);
                SaveDataToFile(UntranslatedRegexPath, _dumpedRegexs, true); // Mengirim flag isRegex = true

                _savePending = false;
            });
        }

        private static void SaveDataToFile(string path, HashSet<string> dataSet, bool isRegex)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");

                string[] currentStrings;

                lock (_lock)
                {
                    currentStrings = new string[dataSet.Count];
                    dataSet.CopyTo(currentStrings);
                }

                for (int i = 0; i < currentStrings.Length; i++)
                {
                    string rawKey = currentStrings[i];
                    string escapedKey = EscapeForJson(rawKey);
                    string escapedValue;

                    // [FITUR BARU] Auto-Formatter untuk Value Regex
                    if (isRegex)
                    {
                        string formatValue = rawKey;
                        int counter = 0;

                        // 1. Mengubah (\d+) menjadi {0}, {1}, dst.
                        string target = @"(\d+)";
                        int index;
                        while ((index = formatValue.IndexOf(target)) != -1)
                        {
                            formatValue = formatValue.Remove(index, target.Length).Insert(index, "{" + counter + "}");
                            counter++;
                        }

                        // 2. Membersihkan pelarian Regex (Unescape) seperti \* menjadi *
                        string[] specialChars = { "\\", "^", "$", ".", "|", "?", "*", "+", "(", ")", "[", "]", "{", "}" };
                        foreach (var ch in specialChars)
                        {
                            formatValue = formatValue.Replace("\\" + ch, ch);
                        }

                        escapedValue = EscapeForJson(formatValue);
                    }
                    else
                    {
                        escapedValue = escapedKey;
                    }

                    sb.Append($"  \"{escapedKey}\": \"{escapedValue}\"");

                    if (i < currentStrings.Length - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }

                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[Dumper] Error saving JSON to {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        private static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}