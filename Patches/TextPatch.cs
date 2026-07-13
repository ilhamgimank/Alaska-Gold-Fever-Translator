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
            try
            {
                if (string.IsNullOrEmpty(value)) return;

                TextDumper.DumpString(value, "UI-Text", false);

                if (TranslationManager.TryGetTranslation(value, out string translatedText) ||
                    TranslationManager.TryGetRegexTranslation(value, out translatedText))
                {
                    value = translatedText;
                }
                value = CurrencyConverter.Convert(value);
            }
            catch { } // Baju besi: Jika error, abaikan dan biarkan game berjalan normal
        }
    }

    // 2. Patch OnEnable UGUI 
    [HarmonyPatch(typeof(Text), "OnEnable")]
    public static class UGUITextOnEnablePatch
    {
        static void Postfix(Text __instance)
        {
            try
            {
                if (__instance == null || string.IsNullOrEmpty(__instance.text)) return;

                TextDumper.DumpString(__instance.text, "UI-Prefab", false);

                string currentText = __instance.text;
                if (TranslationManager.TryGetTranslation(currentText, out string translatedText) ||
                    TranslationManager.TryGetRegexTranslation(currentText, out translatedText))
                {
                    currentText = translatedText;
                }
                currentText = CurrencyConverter.Convert(currentText);

                if (__instance.text != currentText)
                {
                    __instance.text = currentText;
                }
            }
            catch { }
        }
    }

    // 3. Patch TextMeshPro (Versi Bersih, Stabil & Kebal Error)
    public static class TextPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            try
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

                    var onEnableMethod = AccessTools.Method(tmpTextType, "OnEnable");
                    if (onEnableMethod != null)
                    {
                        var postfixOnEnable = typeof(TextPatch).GetMethod(nameof(CatchPrefabText), BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(onEnableMethod, postfix: new HarmonyMethod(postfixOnEnable));
                    }

                    Main.Logger.LogInfo("Clean, Stable & Bulletproof TMP Patches applied successfully.");
                }
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError("FAILED to patch TMPro: " + ex.Message);
            }
        }

        private static void TextSetterPrefix(ref string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return;

                TextDumper.DumpString(value, "TMP-Text", false);

                if (TranslationManager.TryGetTranslation(value, out string translatedText) ||
                    TranslationManager.TryGetRegexTranslation(value, out translatedText))
                {
                    value = translatedText;
                }
                value = CurrencyConverter.Convert(value);
            }
            catch { }
        }

        private static void SetTextPrefix(ref string __0)
        {
            try
            {
                if (string.IsNullOrEmpty(__0)) return;

                TextDumper.DumpString(__0, "TMP-Text", false);

                if (TranslationManager.TryGetTranslation(__0, out string translatedText) ||
                    TranslationManager.TryGetRegexTranslation(__0, out translatedText))
                {
                    __0 = translatedText;
                }
                __0 = CurrencyConverter.Convert(__0);
            }
            catch { }
        }

        private static void CatchPrefabText(Component __instance)
        {
            if (__instance == null) return;
            try
            {
                var prop = __instance.GetType().GetProperty("text");
                if (prop != null)
                {
                    string originalText = prop.GetValue(__instance, null) as string;
                    if (!string.IsNullOrEmpty(originalText))
                    {
                        TextDumper.DumpString(originalText, "TMP-Prefab", false);

                        string newText = originalText;
                        if (TranslationManager.TryGetTranslation(originalText, out string translatedText) ||
                            TranslationManager.TryGetRegexTranslation(originalText, out translatedText))
                        {
                            newText = translatedText;
                        }
                        newText = CurrencyConverter.Convert(newText);

                        if (originalText != newText)
                        {
                            prop.SetValue(__instance, newText, null);
                        }
                    }
                }
            }
            catch { }
        }
    }
}