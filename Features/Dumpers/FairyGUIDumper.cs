// Features/Dumpers/FairyGUIDumper.cs (Dumper untuk library FairyGUI) (Update: Menambahkan Tipe UI)
using HarmonyLib;
using System.Reflection;

namespace AlaskaGoldFeverTranslator.Features.Dumpers
{
    // Sama seperti TMP, FairyGUI adalah library eksternal, jadi patch dipasang secara dinamis
    public static class FairyGUIDumper
    {
        public static void ApplyPatch(Harmony harmony)
        {
            // Mencari kelas GTextField milik modul FairyGUI
            var gTextFieldType = System.Type.GetType("FairyGUI.GTextField, FairyGUI");

            if (gTextFieldType != null)
            {
                // Mengambil method setter dari properti 'text' milik FairyGUI
                var originalSetter = gTextFieldType.GetProperty("text").GetSetMethod();
                var postfix = typeof(FairyGUIDumper).GetMethod(nameof(TextSetterPostfix), BindingFlags.Static | BindingFlags.NonPublic);

                // Menerapkan patch Harmony
                harmony.Patch(originalSetter, postfix: new HarmonyMethod(postfix));
                Main.Logger.LogInfo("FairyGUI Dumper patched successfully.");
            }
            else
            {
                Main.Logger.LogInfo("FairyGUI module not found in this game. Skipping FairyGUI Dumper.");
            }
        }

        // Fungsi yang dipanggil saat FairyGUI me-render/mengubah teks
        private static void TextSetterPostfix(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Mengirimkan teks ke pusat dumper dengan tipe Fairy
                TextDumper.DumpString(value, "Fairy");
            }
        }
    }
}