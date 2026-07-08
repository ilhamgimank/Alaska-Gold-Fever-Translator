// Main.cs (Entry point utama BepInEx Mod)
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features;
using AlaskaGoldFeverTranslator.Features.Dumpers;
using AlaskaGoldFeverTranslator.Patches;

namespace AlaskaGoldFeverTranslator
{
    // Mengubah versi mod ke v0.2.20
    [BepInPlugin("com.ilham.alaskatranslator", "Alaska Gold Fever Translator", "0.2.20")]
    public class Main : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        private Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("Plugin Alaska Gold Fever Translator v0.2.20 is loaded!");

            // 1. Inisialisasi Manajer Inti (Struktur Folder & Memori Teks/Gambar)
            FileManager.Initialize();
            TranslationManager.Initialize();
            TextureManager.Initialize();

            // 2. Inisialisasi Fitur Utama Latar Belakang
            TextDumper.Initialize();
            AutoTranslator.Initialize(); // Sudah termasuk sistem Shift Kanan + T
            LiveUpdater.Initialize();
            SceneScanner.Initialize();
            PathDetector.Initialize();

            // 3. Inisialisasi Sistem Waktu & Jam UI In-Game
            GameTimeManager.Initialize(); // Otak penyedot waktu In-Game
            DigitalClock.Initialize();    // UI Jam Digital (Home)
            AnalogClock.Initialize();     // UI Jam Analog (End)

            // 4. Menerapkan Semua Patch (Pencegatan Engine Unity)
            _harmony = new Harmony("com.ilham.alaskatranslator");

            // Menerapkan patch yang menggunakan atribut [HarmonyPatch] secara otomatis
            _harmony.PatchAll();

            // Menerapkan patch khusus (Dynamic AccessTools)
            TextPatch.ApplyPatch(_harmony);
            ImagePatch.ApplyPatch(_harmony);
            TMPDumper.ApplyPatch(_harmony);
            FairyGUIDumper.ApplyPatch(_harmony);

            Logger.LogInfo("All modules and patches have been successfully initialized! Ready to translate.");
        }
    }
}