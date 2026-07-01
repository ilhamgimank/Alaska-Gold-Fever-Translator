// Managers/FileManager.cs (Fitur pembuatan struktur folder otomatis)
using System.IO;
using BepInEx;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class FileManager
    {
        // Path utama untuk mod ini di dalam folder Plugins
        public static string ModFolderPath { get; private set; }

        // Sub-folder path
        public static string DefaultTexturesPath { get; private set; }
        public static string CustomFontsPath { get; private set; }
        public static string DumpsPath { get; private set; }
        public static string LocalizationPath { get; private set; }

        public static void Initialize()
        {
            // Menentukan path utama mod di folder BepInEx/plugins
            ModFolderPath = Path.Combine(Paths.PluginPath, "Alaska Gold Fever Translator");

            // Menentukan path untuk sub-folder
            DefaultTexturesPath = Path.Combine(ModFolderPath, "[Default Textures]");
            CustomFontsPath = Path.Combine(ModFolderPath, "[Custom Fonts]");
            DumpsPath = Path.Combine(ModFolderPath, "Dumps");
            LocalizationPath = Path.Combine(ModFolderPath, "Localization");

            // Membuat folder jika belum ada
            CreateFolderIfNotExists(ModFolderPath);
            CreateFolderIfNotExists(DefaultTexturesPath);
            CreateFolderIfNotExists(CustomFontsPath);
            CreateFolderIfNotExists(DumpsPath);
            CreateFolderIfNotExists(LocalizationPath);

            // Path spesifik untuk bahasa Indonesia
            string indonesianPath = Path.Combine(LocalizationPath, "Indonesian");
            string indonesianStringsPath = Path.Combine(indonesianPath, "Strings");
            string indonesianTexturesPath = Path.Combine(indonesianPath, "Textures");

            CreateFolderIfNotExists(indonesianPath);
            CreateFolderIfNotExists(indonesianStringsPath);
            CreateFolderIfNotExists(indonesianTexturesPath);

            // Membuat file JSON kosongan di dalam folder Strings sesuai instruksi Phase 1b
            string translationStringsPath = Path.Combine(indonesianStringsPath, "translation_strings.json");
            string translationRegexPath = Path.Combine(indonesianStringsPath, "translation_regexs.json");

            CreateFileIfNotExists(translationStringsPath, "{}");
            CreateFileIfNotExists(translationRegexPath, "{}");

            Main.Logger.LogInfo("All required folders and translation template files have been checked/created successfully.");
        }

        // Method bantuan untuk membuat folder
        private static void CreateFolderIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Main.Logger.LogInfo($"Created directory: {path}");
            }
        }

        // Method bantuan untuk membuat file kosong
        private static void CreateFileIfNotExists(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
                Main.Logger.LogInfo($"Created file: {Path.GetFileName(path)}");
            }
        }
    }
}