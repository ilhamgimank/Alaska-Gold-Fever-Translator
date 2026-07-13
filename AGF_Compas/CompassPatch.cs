// AGF_Compass/CompassPatch.cs (Patch mandiri khusus untuk mencegat teks kompas)
using HarmonyLib;
using UnityEngine.UI;
using AlaskaGoldFeverTranslator.Features;

namespace AlaskaGoldFeverTranslator.Modules
{
    [HarmonyPatch(typeof(Text))]
    public static class CompassPatch
    {
        [HarmonyPatch("text", MethodType.Setter)]
        [HarmonyPrefix]
        static void Prefix(Text __instance, ref string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Gunakan CompassConverter dari mod utama
                value = CompassConverter.Convert(value, __instance);
            }
        }

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void OnEnablePostfix(Text __instance)
        {
            if (__instance != null && !string.IsNullOrEmpty(__instance.text))
            {
                string converted = CompassConverter.Convert(__instance.text, __instance);
                if (__instance.text != converted)
                {
                    __instance.text = converted;
                }
            }
        }
    }
}