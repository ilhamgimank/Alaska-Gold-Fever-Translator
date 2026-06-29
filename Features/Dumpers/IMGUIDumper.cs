// Features/Dumpers/IMGUIDumper.cs (Dumper untuk UI lawas / dev console Unity) (Update: Menambahkan Tipe UI)
using HarmonyLib;
using UnityEngine;

namespace AlaskaGoldFeverTranslator.Features.Dumpers
{
    // IMGUI sering menggunakan struktur GUIContent untuk me-render teks di layar
    [HarmonyPatch(typeof(GUIContent), "text", MethodType.Setter)]
    public static class IMGUIDumper
    {
        // Method ini akan dieksekusi setelah GUI content teks di-set
        static void Postfix(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Mengirimkan teks ke pusat dumper dengan tipe IMGUI
                TextDumper.DumpString(value, "IMGUI");
            }
        }
    }
}