// Features/LiveUpdater.cs (Fitur pembaruan teks UI secara langsung di layar dari Main Thread)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using HarmonyLib;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class LiveUpdater
    {
        // Antrean teks yang baru selesai diterjemahkan oleh pekerja latar belakang
#pragma warning disable
        private static Queue<KeyValuePair<string, string>> _pendingUpdates = new Queue<KeyValuePair<string, string>>();

        // Pengaman agar tidak terjadi bentrok saat Background Thread melempar data ke Main Thread
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            // Membuat GameObject tersembunyi untuk menjalankan siklus Update (Main Thread)
            GameObject updaterObj = new GameObject("Alaska_LiveUpdater");
            Object.DontDestroyOnLoad(updaterObj);
            updaterObj.AddComponent<LiveUpdaterHandler>();

            Main.Logger.LogInfo("Live Updater initialized (Real-time screen updates active).");
        }

        // Dipanggil oleh AutoTranslator dari thread latar belakang
        public static void PushUpdate(string originalText, string translatedText)
        {
            lock (_lock)
            {
                _pendingUpdates.Enqueue(new KeyValuePair<string, string>(originalText, translatedText));
            }
        }

        // Dijalankan secara konstan di Main Thread oleh LiveUpdaterHandler
        public static void ProcessUpdates()
        {
            Dictionary<string, string> updatesToApply = null;

            lock (_lock)
            {
                // Jika ada teks baru yang harus diperbarui di layar
                if (_pendingUpdates.Count > 0)
                {
                    updatesToApply = new Dictionary<string, string>();
                    while (_pendingUpdates.Count > 0)
                    {
                        var item = _pendingUpdates.Dequeue();
                        updatesToApply[item.Key] = item.Value;
                    }
                }
            }

            // Eksekusi perubahan ke komponen yang sedang aktif di layar
            if (updatesToApply != null && updatesToApply.Count > 0)
            {
                ApplyTranslationsToActiveScene(updatesToApply);
            }
        }

        // Memindai layar dan langsung menimpa teks Inggris menjadi Indonesia
        private static void ApplyTranslationsToActiveScene(Dictionary<string, string> updates)
        {
            int replacedCount = 0;

            // 1. Sapuan Kilat: Update UGUI Text
            Text[] allTexts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (Text t in allTexts)
            {
                if (updates.TryGetValue(t.text, out string translatedText))
                {
                    t.text = translatedText;
                    replacedCount++;
                }
            }

            // 2. Sapuan Kilat: Update TextMesh 3D bawaan
            TextMesh[] allMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach (TextMesh tm in allMeshes)
            {
                if (updates.TryGetValue(tm.text, out string translatedText))
                {
                    tm.text = translatedText;
                    replacedCount++;
                }
            }

            // 3. Sapuan Kilat: Update TextMeshPro (TMP) secara Dinamis
            System.Type tmpType = AccessTools.TypeByName("TMPro.TMP_Text");
            if (tmpType != null)
            {
                Object[] allTMPs = Object.FindObjectsByType(tmpType, FindObjectsSortMode.None);
                var prop = tmpType.GetProperty("text");

                if (prop != null)
                {
                    foreach (Object objTmp in allTMPs)
                    {
                        Component tmp = (Component)objTmp;
                        string currentText = prop.GetValue(tmp, null) as string;

                        if (!string.IsNullOrEmpty(currentText) && updates.TryGetValue(currentText, out string translatedText))
                        {
                            prop.SetValue(tmp, translatedText, null);
                            replacedCount++;
                        }
                    }
                }
            }

            if (replacedCount > 0)
            {
                Main.Logger.LogInfo($"[LiveUpdater] Instantly swapped translation for {replacedCount} UI element(s) currently on screen.");
            }
        }
    }

    // Komponen perantara agar fungsi LiveUpdater bisa diikat ke sistem frame per detik (FPS) gamenya Unity
    public class LiveUpdaterHandler : MonoBehaviour
    {
        void Update()
        {
            LiveUpdater.ProcessUpdates();
        }
    }
}