using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace AlaskaGoldFeverTranslator.Managers
{
    // Enum untuk mesin penerjemah agar rapi
    public enum TranslatorEngineType { Google, MyMemory }

    public static class ConfigManager
    {
        public static ConfigFile Config { get; private set; }

        // 1. General
        public static ConfigEntry<string> TargetLanguage;

        // 2. Translator API
        public static ConfigEntry<TranslatorEngineType> ActiveEngine;

        // 3. Developer Tools
        public static ConfigEntry<bool> EnableAutoDumper;
        public static ConfigEntry<bool> EnablePathDetector;

        // 4. Modules (Jam)
        public static ConfigEntry<bool> ShowDigitalClock;
        public static ConfigEntry<bool> ShowAnalogClock;

        public static void Initialize()
        {
            // [UPDATE BARU] Memaksa BepInEx membuat file config dengan nama khusus "com.ilhamgimank.agfmods.cfg"
            string configPath = Path.Combine(Paths.ConfigPath, "com.ilhamgimank.agfmods.cfg");
            Config = new ConfigFile(configPath, true);

            TargetLanguage = Config.Bind("1. General", "TargetLanguage", "Indonesian", "Bahasa target utama untuk mod pelokalan.");

            ActiveEngine = Config.Bind("2. Translator API", "ActiveEngine", TranslatorEngineType.Google, "Mesin penerjemah otomatis yang digunakan (Google / MyMemory).");

            EnableAutoDumper = Config.Bind("3. Developer Tools", "EnableAutoDumper", true, "Menyalakan/mematikan fitur perekaman teks otomatis (Dumper) (Hotkey: F9).");

            EnablePathDetector = Config.Bind("3. Developer Tools", "EnablePathDetector", true, "Menyalakan/mematikan alat pemindai UI (Path Detector).");

            ShowDigitalClock = Config.Bind("4. Modules", "ShowDigitalClock", false, "Tampilkan jam digital di layar secara default (Hotkey: Home).");
            ShowAnalogClock = Config.Bind("4. Modules", "ShowAnalogClock", false, "Tampilkan jam analog di layar secara default (Hotkey: End).");
        }

        // Fungsi khusus untuk dipanggil jika ada perubahan dari dalam game (Hotkey)
        public static void Save()
        {
            if (Config != null) Config.Save();
        }
    }
}