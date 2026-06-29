// Features/SceneScanner.cs (Fitur untuk menyedot teks bawaan scene saat layar baru dimuat)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarmonyLib;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class SceneScanner
    {
        public static void Initialize()
        {
            // Mendaftarkan event agar fungsi OnSceneLoaded dipanggil setiap game pindah layar/menu
            SceneManager.sceneLoaded += OnSceneLoaded;
            Main.Logger.LogInfo("Scene Scanner initialized.");
        }

        // Fungsi ini akan berjalan otomatis setiap scene baru selesai dimuat
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Main.Logger.LogInfo($"[SceneScanner] Scanning loaded scene: {scene.name}");

            int textCount = 0;

            // 1. Scan UGUI Text bawaan (TERMASUK YANG DISEMBUNYIKAN / INACTIVE)
            Text[] allTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Text t in allTexts)
            {
                if (!string.IsNullOrEmpty(t.text))
                {
                    TextDumper.DumpString(t.text, "Scene-UI", false);
                    textCount++;
                }
            }

            // 2. Scan TextMesh 3D bawaan (TERMASUK INACTIVE)
            TextMesh[] allMesh = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TextMesh tm in allMesh)
            {
                if (!string.IsNullOrEmpty(tm.text))
                {
                    TextDumper.DumpString(tm.text, "Scene-TextMesh", false);
                    textCount++;
                }
            }

            // 3. Scan TextMeshPro Secara Dinamis (TERMASUK INACTIVE)
            System.Type tmpType = AccessTools.TypeByName("TMPro.TMP_Text");
            if (tmpType != null)
            {
                Object[] allTMPs = Object.FindObjectsByType(tmpType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Object objTmp in allTMPs)
                {
                    Component tmp = (Component)objTmp;
                    var prop = tmpType.GetProperty("text");
                    string txtVal = prop?.GetValue(tmp, null) as string;

                    if (!string.IsNullOrEmpty(txtVal))
                    {
                        TextDumper.DumpString(txtVal, "Scene-TMP", false);
                        textCount++;
                    }
                }
            }

            Main.Logger.LogInfo($"[SceneScanner] Found {textCount} potential texts in scene {scene.name} (Including Inactive UI)");
        }
    }
}