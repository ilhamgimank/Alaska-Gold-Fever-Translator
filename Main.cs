using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features;
using AlaskaGoldFeverTranslator.Features.Dumpers;
using AlaskaGoldFeverTranslator.Patches;

namespace AlaskaGoldFeverTranslator
{
    // [UPDATE] GUID baru untuk mod utama
    [BepInPlugin("com.ilhamgimank.agftranslator", "Alaska Gold Fever Translator Core", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        private Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("Plugin Alaska Gold Fever Translator CORE v1.0.0 is loaded!");

            // 0. Inisialisasi Config System (Sekarang dipanggil tanpa parameter karena pakai file khusus)
            ConfigManager.Initialize();

            // 1. Inisialisasi Manajer Inti
            FileManager.Initialize();
            TranslationManager.Initialize();
            TextureManager.Initialize();

            // 2. Inisialisasi Fitur Utama
            TextDumper.Initialize();
            LiveUpdater.Initialize();
            SceneScanner.Initialize();
            PathDetector.Initialize();

            // 3. Menerapkan Semua Patch dengan GUID baru
            _harmony = new Harmony("com.ilhamgimank.agftranslator");
            _harmony.PatchAll();

            TextPatch.ApplyPatch(_harmony);
            ImagePatch.ApplyPatch(_harmony);
            TMPDumper.ApplyPatch(_harmony);
            FairyGUIDumper.ApplyPatch(_harmony);

            Logger.LogInfo("Translator Core modules and patches successfully initialized!");
        }
    }
}