// Patches/TextPatch.cs (Fitur untuk menerapkan terjemahan ke teks in-game)
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using System.Reflection;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features;

namespace AlaskaGoldFeverTranslator.Patches
{
    // 1. Patch Setter UGUI (Untuk teks yang berubah di tengah permainan)
    [HarmonyPatch(typeof(Text), "text", MethodType.Setter)]
    public static class UGUITextSetterPatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Prefix(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (TranslationManager.TranslatedStrings.TryGetValue(value, out string translatedText))
            {
                value = translatedText;
            }
        }
    }

    // 2. Patch OnEnable UGUI (Menangkap teks bawaan Inspector/Prefab yang langsung muncul)
    [HarmonyPatch(typeof(Text), "OnEnable")]
    public static class UGUITextOnEnablePatch
    {
        static void Postfix(Text __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.text)) return;

            TextDumper.DumpString(__instance.text, "UI-Prefab", false);

            if (TranslationManager.TranslatedStrings.TryGetValue(__instance.text, out string translatedText))
            {
                __instance.text = translatedText;
            }
        }
    }

    // 3. Patch Ekstrim TextMeshPro (Dikelola terpusat oleh ApplyPatch di Main.cs)
    public static class TextPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            var tmpTextType = AccessTools.TypeByName("TMPro.TMP_Text");
            if (tmpTextType != null)
            {
                // A. Mencegat Setter Properti (text = "...")
                var setter = AccessTools.PropertySetter(tmpTextType, "text");
                if (setter != null)
                {
                    var prefixSetter = typeof(TextPatch).GetMethod(nameof(TextSetterPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(setter, prefix: new HarmonyMethod(prefixSetter));
                }

                // B. Mencegat Metode SetText(string) yang sering dipakai developer game
                var setTextMethod = AccessTools.Method(tmpTextType, "SetText", new System.Type[] { typeof(string) });
                if (setTextMethod != null)
                {
                    var prefixSetText = typeof(TextPatch).GetMethod(nameof(SetTextPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(setTextMethod, prefix: new HarmonyMethod(prefixSetText));
                }

                var catchMethod = typeof(TextPatch).GetMethod(nameof(CatchPrefabText), BindingFlags.Static | BindingFlags.NonPublic);

                // C. Mencegat semua metode saat objek diciptakan dan dimunculkan ke layar
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

                Main.Logger.LogInfo("Extreme TMP Translation Patches applied successfully.");
            }
            else
            {
                Main.Logger.LogError("FAILED to find TMPro.TMP_Text! Game might be using a heavily modified TextMeshPro.");
            }
        }

        private static void TextSetterPrefix(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (TranslationManager.TranslatedStrings.TryGetValue(value, out string translatedText))
            {
                value = translatedText;
            }
        }

        // Penjelasan: __0 adalah cara ajaib Harmony untuk mengambil parameter pertama dari method asli (SetText)
        private static void SetTextPrefix(ref string __0)
        {
            if (string.IsNullOrEmpty(__0)) return;

            if (TranslationManager.TranslatedStrings.TryGetValue(__0, out string translatedText))
            {
                __0 = translatedText;
            }
        }

        // Dipanggil otomatis saat Awake, OnEnable, atau Start berjalan di game
        private static void CatchPrefabText(Component __instance)
        {
            if (__instance == null) return;

            var prop = AccessTools.Property(__instance.GetType(), "text");
            if (prop != null)
            {
                string originalText = prop.GetValue(__instance, null) as string;
                if (!string.IsNullOrEmpty(originalText))
                {
                    TextDumper.DumpString(originalText, "TMP-Prefab", false);

                    if (TranslationManager.TranslatedStrings.TryGetValue(originalText, out string translatedText))
                    {
                        prop.SetValue(__instance, translatedText, null);
                    }
                }
            }
        }
    }
}