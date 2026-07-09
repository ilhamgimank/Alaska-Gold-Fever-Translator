// Features/AutoTranslator.cs (Sistem antrean penerjemahan otomatis di latar belakang)
using AlaskaGoldFeverTranslator.Managers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class AutoTranslator
    {
        // Struktur data untuk menyimpan tugas terjemahan
        private struct TranslationTask
        {
            public string RawText;
            public bool IsRegex;
            public string RegexKey;
        }

        // Antrean tugas terjemahan
#pragma warning disable
        private static Queue<TranslationTask> _translationQueue = new Queue<TranslationTask>();
        private static bool _isTranslating = false;

        // [FITUR BARU] Enum untuk melacak mesin penerjemah yang aktif
        public enum TranslationEngine { Google, MyMemory }
        public static TranslationEngine ActiveEngine = TranslationEngine.Google; // Default pakai Google

        public static void Initialize()
        {
            // Memasang pendeteksi tombol ke dalam game
            GameObject handlerObj = new GameObject("Alaska_AutoTranslatorHandler");
            UnityEngine.Object.DontDestroyOnLoad(handlerObj);
            handlerObj.AddComponent<AutoTranslatorHandler>();

            Main.Logger.LogInfo($"Auto Translator queue system initialized. Active Engine: {ActiveEngine}");
        }

        // Method ini dipanggil oleh TextDumper
        public static void AddToQueue(string originalText, bool isRegex = false, string regexKey = null)
        {
            // Jangan masukkan jika teks sudah ada di dalam antrean memori
            if (TranslationManager.TranslatedStrings.ContainsKey(originalText)) return;

            _translationQueue.Enqueue(new TranslationTask { RawText = originalText, IsRegex = isRegex, RegexKey = regexKey });
            Main.Logger.LogInfo($"[AutoTranslator] Added to queue: \"{originalText}\" (Regex: {isRegex})");

            if (!_isTranslating)
            {
                Task.Run(ProcessQueueAsync);
            }
        }

        // Pekerja latar belakang
        private static async Task ProcessQueueAsync()
        {
            _isTranslating = true;

            while (_translationQueue.Count > 0)
            {
                TranslationTask task = _translationQueue.Dequeue();

                // Jeda 3 detik untuk menghindari IP diblokir API
                await Task.Delay(3000);

                string translatedText = null;

                // [FITUR BARU] Mengeksekusi mesin terjemahan sesuai dengan pilihan yang sedang aktif
                if (ActiveEngine == TranslationEngine.Google)
                {
                    translatedText = await TranslatorEngine.GoogleTranslate.TranslateAsync(task.RawText, "en", "id");
                }
                else if (ActiveEngine == TranslationEngine.MyMemory)
                {
                    translatedText = await TranslatorEngine.MyMemoryTranslate.TranslateAsync(task.RawText, "en", "id");
                }

                if (!string.IsNullOrEmpty(translatedText))
                {
                    if (task.IsRegex)
                    {
                        // Menyulap angka di terjemahan menjadi parameter penempatan {0}, {1}
                        string formatValue = translatedText;
                        int counter = 0;

                        // Regex ini akan mengganti semua angka murni dengan urutan parameter {0}, {1}, dst.
                        formatValue = Regex.Replace(formatValue, @"\d+", match => "{" + (counter++) + "}");

                        TranslationManager.AddAndSaveRegexTranslation(task.RegexKey, formatValue);
                        LiveUpdater.PushUpdate(task.RawText, translatedText);

                        // [PERBAIKAN UX] Menampilkan log hasil format Regex yang sesungguhnya!
                        Main.Logger.LogInfo($"[AutoTranslator] Regex Saved: \"{task.RegexKey}\" -> \"{formatValue}\"");
                    }
                    else
                    {
                        TranslationManager.AddAndSaveTranslation(task.RawText, translatedText);
                        LiveUpdater.PushUpdate(task.RawText, translatedText);

                        // Log untuk teks statis biasa
                        Main.Logger.LogInfo($"[AutoTranslator] Static Success: \"{task.RawText}\" -> \"{translatedText}\"");
                    }
                }
            }

            _isTranslating = false;
        }
    }

    // [FITUR BARU] Handler untuk menangkap kombinasi tombol ganti mesin
    public class AutoTranslatorHandler : MonoBehaviour
    {
        void Update()
        {
            // Cek jika tombol Shift Kanan ditahan dan tombol T ditekan
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.T))
            {
                // Menukar mesin (Tukar (Toggle))
                AutoTranslator.ActiveEngine = AutoTranslator.ActiveEngine == AutoTranslator.TranslationEngine.Google
                    ? AutoTranslator.TranslationEngine.MyMemory
                    : AutoTranslator.TranslationEngine.Google;

                Main.Logger.LogInfo($"[AutoTranslator] 🔄 Translation Engine switched to: {AutoTranslator.ActiveEngine}");
            }
        }
    }
}