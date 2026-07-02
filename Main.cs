// Main.cs (Inisialisasi awal mod dan BepInEx)
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AlaskaGoldFeverTranslator
{
    // Deklarasi metadata BepInEx
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Main : BaseUnityPlugin
    {
        // Struktur statis untuk info plugin
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.ilhamgimank.alaskagoldfever.translator";
            public const string PLUGIN_NAME = "Alaska Gold Fever Translator";
            public const string PLUGIN_VERSION = "0.2.14"; // Live Market Edition
        }

        // Variabel statis agar bisa diakses dari class lain
        public static Main Instance { get; private set; }
        internal new static ManualLogSource Logger { get; private set; }

        // Objek Harmony untuk mengeksekusi sistem patch dumper
        private Harmony _harmony;

        private void Awake()
        {
            // Set instance dan logger
            Instance = this;
            Logger = base.Logger;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            // Memanggil Path Detector untuk mendapatkan info game
            Features.PathDetector.Initialize();

            // Memanggil File Manager untuk membuat struktur folder
            Managers.FileManager.Initialize();

            // Memanggil Text Dumper dan Translation Manager
            Features.TextDumper.Initialize();
            Managers.TranslationManager.Initialize();

            // [PERBAIKAN] Menyalakan Texture Manager yang sempat terlupakan!
            Managers.TextureManager.Initialize();

            // Memanggil Scene Scanner
            Features.SceneScanner.Initialize();

            // Menginisialisasi Auto Translator & Live Updater
            Features.AutoTranslator.Initialize();
            Features.LiveUpdater.Initialize();

            // [FITUR BARU] Menyalakan Web Scraper untuk Live Market Currency
            Features.CurrencyConverter.Initialize();

            // Menginisialisasi Harmony dan menerapkan semua dumper UI standar
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            Logger.LogInfo("Harmony successfully patched automatic UI dumpers.");

            // Menerapkan patch manual (Dinamis)
            Features.Dumpers.FairyGUIDumper.ApplyPatch(_harmony);
            Patches.TextPatch.ApplyPatch(_harmony);

            // [PERBAIKAN] Menyalakan Pencegat Gambar (Image Patch) ke dalam game!
            Patches.ImagePatch.ApplyPatch(_harmony);

            Logger.LogInfo("Update v0.2.14 initialization complete. All Systems Active (Live Market Edition).");
        }
    }
}