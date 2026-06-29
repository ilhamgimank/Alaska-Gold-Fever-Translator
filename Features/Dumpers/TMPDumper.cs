// Features/Dumpers/TMPDumper.cs (Dumper untuk TextMeshPro)
using HarmonyLib;
using System.Reflection;

namespace AlaskaGoldFeverTranslator.Features.Dumpers
{
    public static class TMPDumper
    {
        public static void ApplyPatch(Harmony harmony)
        {
            // [UPDATE BARU] Menggunakan AccessTools agar kebal terhadap perubahan nama assembly oleh developer!
            var tmpTextType = AccessTools.TypeByName("TMPro.TMP_Text");

            if (tmpTextType != null)
            {
                var originalSetter = AccessTools.PropertySetter(tmpTextType, "text");
                if (originalSetter != null)
                {
                    var postfix = typeof(TMPDumper).GetMethod(nameof(TextSetterPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(originalSetter, postfix: new HarmonyMethod(postfix));
                    Main.Logger.LogInfo("TMP Dumper patched successfully via AccessTools.");
                }
            }
            else
            {
                Main.Logger.LogWarning("TextMeshPro module not found. Skipping TMP Dumper.");
            }
        }

        private static void TextSetterPostfix(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                TextDumper.DumpString(value, "TMP", false);
            }
        }
    }
}