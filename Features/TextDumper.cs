// Features/TextDumper.cs (Fitur dumper teks utama) (Update: Filter Anti-Duplikat Terjemahan)
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public static void Initialize()
        {
            UntranslatedStringsPath = Path.Combine(FileManager.DumpsPath, "untranslation_strings.json");
            UntranslatedRegexPath = Path.Combine(FileManager.DumpsPath, "untranslation_regexs.json");

            CreateJsonFileIfNotExists(UntranslatedStringsPath, "{\n}");
            CreateJsonFileIfNotExists(UntranslatedRegexPath, "{\n}");

            Main.Logger.LogInfo("Text Dumper initialized and JSON files are ready.");
        }

        private static void CreateJsonFileIfNotExists(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
                Main.Logger.LogInfo($"Created dump file: {Path.GetFileName(path)}");
            }
        }

        private static bool IsSpamText(string text, string uiType)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (text.Contains("Lorem ipsum") || text.Contains("Lorem Ipsum") || text.StartsWith("Lorem")) return true;
            if (text.Contains("/")) return true;
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

        public static void DumpString(string text, string uiType, bool isRegex = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (IsSpamText(text, uiType)) return;

            // [FITUR BARU] Filter mutlak agar Dumper TIDAK MENCATAT teks yang sudah ada di memori terjemahan
            // Mencegah Teks Inggris yang sudah diterjemahkan masuk lagi ke file dump
            if (TranslationManager.TranslatedStrings.ContainsKey(text)) return;

            // Mencegah Teks Bahasa Indonesia (hasil terjemahan) dianggap sebagai teks baru dan masuk ke file dump
            if (TranslationManager.TranslatedValues.Contains(text)) return;

            bool isNew = false;

            lock (_lock)
            {
                if (isRegex)
                {
                    if (!_dumpedRegexs.Contains(text))
                    {
                        _dumpedRegexs.Add(text);
                        isNew = true;
                    }
                }
                else
                {
                    if (!_dumpedStrings.Contains(text))
                    {
                        _dumpedStrings.Add(text);
                        isNew = true;
                    }
                }
            }

            // Jika ini adalah teks baru yang belum pernah di-dump, masukkan ke antrean AutoTranslator!
            if (isNew)
            {
                string textType = isRegex ? "Regex" : "Static Text";
                string fileName = isRegex ? "untranslation_regexs.json" : "untranslation_strings.json";

                Main.Logger.LogInfo($"[{uiType}][New {textType} Dumped] \"{text}\", added to {fileName}");

                if (isRegex)
                {
                    Task.Run(() => SaveDataToFile(UntranslatedRegexPath, _dumpedRegexs));
                }
                else
                {
                    Task.Run(() => SaveDataToFile(UntranslatedStringsPath, _dumpedStrings));

                    // Kirim ke robot Google Translate
                    AutoTranslator.AddToQueue(text);
                }
            }
        }

        private static void SaveDataToFile(string path, HashSet<string> dataSet)
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
                    string escapedKey = EscapeForJson(currentStrings[i]);
                    sb.Append($"  \"{escapedKey}\": \"{escapedKey}\"");

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