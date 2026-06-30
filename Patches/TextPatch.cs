// Patches/TextPatch.cs (Fitur untuk menerapkan terjemahan ke teks in-game secara instan)
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using System.Reflection;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features;

namespace AlaskaGoldFeverTranslator.Patches
{
    // 1. Patch Setter UGUI
    [HarmonyPatch(typeof(Text), "text", MethodType.Setter)]
    public static class UGUITextSetterPatch
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            // [FITUR BARU] Menyedot teks SEBELUM diterjemahkan secara real-time! Sangat cepat untuk percakapan.
            TextDumper.DumpString(value, "UI-Text", false);

            if (TranslationManager.TryTranslate(value, out string translatedText))
            {
                value = translatedText;
            }
        }
    }

    // 2. Patch OnEnable UGUI 
    [HarmonyPatch(typeof(Text), "OnEnable")]
    public static class UGUITextOnEnablePatch
    {
        static void Postfix(Text __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.text)) return;

            TextDumper.DumpString(__instance.text, "UI-Prefab", false);

            if (TranslationManager.TryTranslate(__instance.text, out string translatedText))
            {
                __instance.text = translatedText;
            }
        }
    }

    // 3. Patch Ekstrim TextMeshPro
    public static class TextPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            var tmpTextType = AccessTools.TypeByName("TMPro.TMP_Text");
            if (tmpTextType != null)
            {
                var setter = AccessTools.PropertySetter(tmpTextType, "text");
                if (setter != null)
                {
                    var prefixSetter = typeof(TextPatch).GetMethod(nameof(TextSetterPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(setter, prefix: new HarmonyMethod(prefixSetter));
                }

                var setTextMethod = AccessTools.Method(tmpTextType, "SetText", new System.Type[] { typeof(string) });
                if (setTextMethod != null)
                {
                    var prefixSetText = typeof(TextPatch).GetMethod(nameof(SetTextPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(setTextMethod, prefix: new HarmonyMethod(prefixSetText));
                }

                var catchMethod = typeof(TextPatch).GetMethod(nameof(CatchPrefabText), BindingFlags.Static | BindingFlags.NonPublic);

                MethodInfo[] lifecycleMethods = {
                    AccessTools.Method(tmpTextType, "Awake"),
                    AccessTools.Method(tmpTextType, "OnEnable"),
                    AccessTools.Method(tmpTextType, "Start")
                };

                foreach (var method in lifecycleMethods)
                {
                    if (method != null)
                    {
                        harmony.Patch(method, postfix: new HarmonyMethod(catchMethod));
                    }
                }

                Main.Logger.LogInfo("Extreme TMP Translation & Real-time Dumper Patches applied successfully.");
            }
            else
            {
                Main.Logger.LogError("FAILED to find TMPro.TMP_Text!");
            }
        }

        private static void TextSetterPrefix(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            // [FITUR BARU] Menyedot teks TMP dinamis seketika (Cocok untuk subtitle/percakapan cepat)
            TextDumper.DumpString(value, "TMP-Text", false);

            if (TranslationManager.TryTranslate(value, out string translatedText))
            {
                value = translatedText;
            }
        }

        private static void SetTextPrefix(ref string __0)
        {
            if (string.IsNullOrEmpty(__0)) return;

            // [FITUR BARU] Menyedot teks TMP dinamis seketika
            TextDumper.DumpString(__0, "TMP-Text", false);

            if (TranslationManager.TryTranslate(__0, out string translatedText))
            {
                __0 = translatedText;
            }
        }

        private static void CatchPrefabText(Component __instance)
        {
            if (__instance == null) return;

            // Menggunakan C# Reflection standar (GetProperty) alih-alih AccessTools
            // untuk mencegah HarmonyX meneriakkan [Warning] saat objek tidak punya teks.
            var prop = __instance.GetType().GetProperty("text");
            if (prop != null)
            {
                string originalText = prop.GetValue(__instance, null) as string;
                if (!string.IsNullOrEmpty(originalText))
                {
                    TextDumper.DumpString(originalText, "TMP-Prefab", false);

                    if (TranslationManager.TryTranslate(originalText, out string translatedText))
                    {
                        prop.SetValue(__instance, translatedText, null);
                    }
                }
            }
        }
    }
}