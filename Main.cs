// Main.cs (Inisialisasi awal mod dan BepInEx) (Update: Fase 2 - Fix Dumper Duplication)
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
            public const string PLUGIN_VERSION = "0.1.0"; // Naik versi (Fase 2 Selesai: Auto Translator & Deduplication)
        }

        public static Main Instance { get; private set; }
        internal new static ManualLogSource Logger { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            Features.PathDetector.Initialize();
            Managers.FileManager.Initialize();
            Features.TextDumper.Initialize();
            Managers.TranslationManager.Initialize();
            Features.SceneScanner.Initialize();

            // Inisialisasi Auto Translator
            Features.AutoTranslator.Initialize();

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            Logger.LogInfo("Harmony successfully patched automatic UI dumpers.");

            Features.Dumpers.FairyGUIDumper.ApplyPatch(_harmony);
            Patches.TextPatch.ApplyPatch(_harmony);

            Logger.LogInfo("Phase 2 initialization complete (v0.0.9).");
        }
    }
}