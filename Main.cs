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
            public const string PLUGIN_VERSION = "0.2.0"; // Naik versi besar: Fase 3 (Texture Replacer)
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

            // Memanggil Text Dumper, Translation Manager, dan TEXTURE MANAGER
            Features.TextDumper.Initialize();
            Managers.TranslationManager.Initialize();
            Managers.TextureManager.Initialize(); // Inisialisasi pengelola gambar

            // Memanggil Scene Scanner
            Features.SceneScanner.Initialize();

            // Menginisialisasi Auto Translator & Live Updater
            Features.AutoTranslator.Initialize();
            Features.LiveUpdater.Initialize();

            // Menginisialisasi Harmony dan menerapkan semua dumper UI standar
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            Logger.LogInfo("Harmony successfully patched automatic UI dumpers.");

            // Menerapkan patch manual (Dinamis)
            Features.Dumpers.FairyGUIDumper.ApplyPatch(_harmony);
            Patches.TextPatch.ApplyPatch(_harmony);

            // Menerapkan Patch Gambar (Fase 3)
            Patches.ImagePatch.ApplyPatch(_harmony);

            Logger.LogInfo("Phase 3 update (v0.2.0) initialization complete. Texture Engine Active.");
        }
    }
}