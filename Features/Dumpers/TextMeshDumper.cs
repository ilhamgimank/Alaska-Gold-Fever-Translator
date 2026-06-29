// Features/Dumpers/TextMeshDumper.cs (Dumper untuk komponen TextMesh 3D bawaan) (Update: Menambahkan Tipe UI)
using HarmonyLib;
using UnityEngine;

namespace AlaskaGoldFeverTranslator.Features.Dumpers
{
    // Patch untuk menangkap teks TextMesh bawaan Unity
    [HarmonyPatch(typeof(TextMesh), "text", MethodType.Setter)]
    public static class TextMeshDumper
    {
        // Method ini akan dieksekusi setelah komponen 3D text di-set
        static void Postfix(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Mengirimkan teks ke pusat dumper dengan tipe TextMesh
                TextDumper.DumpString(value, "TextMesh");
            }
        }
    }
}