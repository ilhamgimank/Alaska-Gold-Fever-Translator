// Main.cs (Initial initialization of the mod and BepInEx)
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AlaskaGoldFeverTranslator
{
    // BepInEx metadata declaration
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Main : BaseUnityPlugin
    {
        // Static structure for plugin info
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.ilhamgimank.alaskagoldfever.translator";
            public const string PLUGIN_NAME = "Alaska Gold Fever Translator";
            public const string PLUGIN_VERSION = "0.2.17"; // Compass Fix Edition
        }

        // Static variables to be accessible from other classes
        public static Main Instance { get; private set; }
        internal new static ManualLogSource Logger { get; private set; }

        // Harmony object to execute the dumper patch system
        private Harmony _harmony;

        private void Awake()
        {
            // Set instance and logger
            Instance = this;
            Logger = base.Logger;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            // Call Path Detector to get game info
            Features.PathDetector.Initialize();

            // Call File Manager to create folder structure
            Managers.FileManager.Initialize();

            // Call Text Dumper and Translation Manager
            Features.TextDumper.Initialize();
            Managers.TranslationManager.Initialize();

            // [FIX] Enable the previously forgotten Texture Manager!
            Managers.TextureManager.Initialize();

            // Call Scene Scanner
            Features.SceneScanner.Initialize();

            // Initialize Auto Translator & Live Updater
            Features.AutoTranslator.Initialize();
            Features.LiveUpdater.Initialize();

            // [NEW FEATURE] Enable Web Scraper for Live Market Currency
            Features.CurrencyConverter.Initialize();

            // Initialize Harmony and apply all standard UI dumpers
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            Logger.LogInfo("Harmony successfully patched automatic UI dumpers.");

            // Apply manual patches (Dynamic)
            Features.Dumpers.FairyGUIDumper.ApplyPatch(_harmony);
            Patches.TextPatch.ApplyPatch(_harmony);

            // [FIX] Enable Image Interceptor (Image Patch) into the game!
            Patches.ImagePatch.ApplyPatch(_harmony);

            Logger.LogInfo("Update v0.2.16 initialization complete. All Systems Active (Dev Mode Edition).");
        }
    }
}