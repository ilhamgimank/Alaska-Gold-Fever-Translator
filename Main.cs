using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features;
using AlaskaGoldFeverTranslator.Features.Dumpers;
using AlaskaGoldFeverTranslator.Patches;

namespace AlaskaGoldFeverTranslator
{
    // Versi 1.0.0 karena arsitektur modular baru yang canggih!
    [BepInPlugin("com.ilham.alaskatranslator", "Alaska Gold Fever Translator Core", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        private Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("Plugin Alaska Gold Fever Translator CORE v1.0.0 is loaded!");

            // 1. Inisialisasi Manajer Inti (Struktur Folder & Memori Teks/Gambar)
            FileManager.Initialize();
            TranslationManager.Initialize();
            TextureManager.Initialize();

            // 2. Inisialisasi Fitur Utama
            TextDumper.Initialize();
            LiveUpdater.Initialize();
            SceneScanner.Initialize();
            PathDetector.Initialize();

            // CATATAN PENTING MODULAR: 
            // GameTimeManager, DigitalClock, AnalogClock, AutoTranslator, dan CompassPatch 
            // TIDAK LAGI diinisialisasi di sini. Mereka hidup mandiri di modul DLL mereka masing-masing!

            // 3. Menerapkan Semua Patch (Pencegatan Engine Unity)
            _harmony = new Harmony("com.ilham.alaskatranslator");

            // Menerapkan patch yang menggunakan atribut [HarmonyPatch] secara otomatis (seperti UGUIDumper)
            _harmony.PatchAll();

            // Menerapkan patch khusus (Dynamic AccessTools)
            TextPatch.ApplyPatch(_harmony);
            ImagePatch.ApplyPatch(_harmony);
            TMPDumper.ApplyPatch(_harmony);
            FairyGUIDumper.ApplyPatch(_harmony);

            Logger.LogInfo("Translator Core modules and patches successfully initialized!");
        }
    }
}