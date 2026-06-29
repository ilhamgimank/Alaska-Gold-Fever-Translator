// Features/Dumpers/UGUIDumper.cs (Dumper untuk UI standar Unity) (Update: Menambahkan Tipe UI)
using HarmonyLib;
using UnityEngine.UI;

namespace AlaskaGoldFeverTranslator.Features.Dumpers
{
    // Mendeklarasikan patch untuk komponen Text bawaan Unity (UGUI)
    [HarmonyPatch(typeof(Text), "text", MethodType.Setter)]
    public static class UGUIDumper
    {
        // Menggunakan Prefix dengan Prioritas Pertama agar selalu mendapatkan teks ASLI
        // sebelum diubah oleh patch terjemahan.
        [HarmonyPriority(Priority.First)]
        static void Prefix(ref string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Mengirimkan teks ke pusat dumper dengan tipe UI
                TextDumper.DumpString(value, "UI");
            }
        }
    }
}