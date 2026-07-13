using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AlaskaGoldFeverTranslator.Managers;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class TextDumper
    {
        // [MODULAR EVENT] Event jembatan agar Mod AutoTranslator bisa mendengarkan teks baru tanpa saling mengikat!
        public static event Action<string, bool, string> OnTextDumped;

        public static string UntranslatedStringsPath { get; private set; }
        public static string UntranslatedRegexPath { get; private set; }

        public static bool IsPaused = false;

#pragma warning disable
        private static HashSet<string> _dumpedStrings = new HashSet<string>();
        private static HashSet<string> _dumpedRegexs = new HashSet<string>();

        private static readonly object _lock = new object();
        private static bool _savePending = false;

        public static void Initialize()
        {
            UntranslatedStringsPath = Path.Combine(FileManager.DumpsPath, "untranslation_strings.json");
            UntranslatedRegexPath = Path.Combine(FileManager.DumpsPath, "untranslation_regexs.json");

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

        private static bool IsIndonesianText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // Indonesian stopwords dan kata-kata translasi sangat umum yang mustahil ada di game Inggris asli
            string[] idWords = {
                "yang", "untuk", "dengan", "adalah", "bisa", "pada", "dari", "dalam",
                "akan", "sudah", "telah", "tidak", "bukan", "atau", "hanya", "jika",
                "bila", "saya", "anda", "kamu", "kita", "kami", "mereka", "emas",
                "tambang", "beliung", "uang", "pertanian", "membeli", "kumpulkan",
                "tukarkan", "memiliki", "cukup", "tunai", "mulai", "menambang", "lengkapi"
            };

            // Bersihkan teks dari simbol dan angka untuk pemindaian kata murni
            string clean = Regex.Replace(text.ToLower(), @"[^a-z\s]", " ");
            string[] tokens = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                foreach (var idWord in idWords)
                {
                    if (token == idWord) return true;
                }
            }
            return false;
        }

        private static bool IsSpamText(string text, string uiType)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (text.Contains("Lorem ipsum") || text.Contains("Lorem Ipsum") || text.StartsWith("Lorem")) return true;
            if (text.Contains("/") && !Regex.IsMatch(text, @"[a-zA-Z]")) return true;

            // [SISTEM FILTER ANTI-CRASH & ANTI-BOUNCING DEWA]
            // Blokir total jika teks mengandung mata uang IDR hasil konversi "Rp." atau "IDR"
            if (Regex.IsMatch(text, @"\b[Rr]p\b") || text.Contains("Rp.") || text.Contains("Rp ") || text.Contains("(Rp") || text.Contains("IDR")) return true;

            // Blokir total jika teks sudah terdeteksi sebagai bahasa Indonesia hasil translasi
            if (IsIndonesianText(text)) return true;

            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^Slot\s+\d+$")) return true;
            if (text.Length <= 1 && char.IsDigit(text[0])) return true;

            string[] modKeywords = new string[] {
                "AGFCore", "AGFMods", "ClockMod", "Clock Mod", "BepInEx", "UnityExplorer",
                "DALAM GAME", "LOKAL", "--:--:--",
                "ContestTimer", "FastTravel", "MinecartUpgrade", "MineReset",
                "MinersLantern", "MoreGems", "TrackBreak", "TreeFeller",
                "WorkerDays", "WorkerPassThrough", "WorkerPayments"
            };

            foreach (var word in modKeywords)
            {
                if (text.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

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
                if (text.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
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
            if (IsPaused) return;

            if (string.IsNullOrWhiteSpace(text)) return;
            if (IsSpamText(text, uiType)) return;

            if (TranslationManager.TranslatedStrings.ContainsKey(text)) return;
            if (TranslationManager.TranslatedValues.Contains(text)) return;

            bool hasLetter = Regex.IsMatch(text, @"[a-zA-Z]");
            bool hasNumber = Regex.IsMatch(text, @"\d+");

            if (hasNumber && !hasLetter) return;

            if (hasNumber && hasLetter)
            {
                string safePattern = EscapeForRegex(text);

                string regexKey = Regex.Replace(safePattern, @"<[^>]+>|\d+", match =>
                {
                    if (match.Value.StartsWith("<") && match.Value.EndsWith(">"))
                        return match.Value;

                    return @"(\d+)";
                });

                if (regexKey != safePattern)
                {
                    if (TranslationManager.TranslatedRegexs.ContainsKey(regexKey)) return;
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
                        // [MODULAR UPDATE] Berteriak lewat event (Mod AutoTranslator akan mendengarnya)
                        OnTextDumped?.Invoke(text, true, regexKey);
                    }
                    return;
                }
            }

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
                // [MODULAR UPDATE] Berteriak lewat event
                OnTextDumped?.Invoke(text, false, null);
            }
        }

        private static void RequestSave()
        {
            if (_savePending) return;
            _savePending = true;

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                SaveDataToFile(UntranslatedStringsPath, _dumpedStrings, false);
                SaveDataToFile(UntranslatedRegexPath, _dumpedRegexs, true);
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

                    if (isRegex)
                    {
                        string formatValue = rawKey;
                        int counter = 0;

                        string target = @"(\d+)";
                        int index;
                        while ((index = formatValue.IndexOf(target)) != -1)
                        {
                            formatValue = formatValue.Remove(index, target.Length).Insert(index, "{" + counter + "}");
                            counter++;
                        }

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