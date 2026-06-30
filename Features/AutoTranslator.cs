// Features/AutoTranslator.cs (Sistem antrean penerjemahan otomatis di latar belakang)
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AlaskaGoldFeverTranslator.Managers;

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

        public static void Initialize()
        {
            Main.Logger.LogInfo("Auto Translator queue system initialized (Regex Supported).");
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

                // Jeda 2 detik untuk menghindari IP diblokir Google
                await Task.Delay(2000);

                // Menerjemahkan teks MENTAH (yang masih ada angkanya) secara natural
                string translatedText = await TranslatorEngine.GoogleTranslate.TranslateAsync(task.RawText, "en", "id");

                if (!string.IsNullOrEmpty(translatedText))
                {
                    Main.Logger.LogInfo($"[AutoTranslator] Success: \"{task.RawText}\" -> \"{translatedText}\"");

                    if (task.IsRegex)
                    {
                        // Menyulap angka di terjemahan menjadi parameter penempatan {0}, {1}
                        string formatValue = translatedText;
                        int counter = 0;

                        // Regex ini akan mengganti semua angka murni dengan urutan parameter {0}, {1}, dst.
                        formatValue = Regex.Replace(formatValue, @"\d+", match => "{" + (counter++) + "}");

                        TranslationManager.AddAndSaveRegexTranslation(task.RegexKey, formatValue);
                        LiveUpdater.PushUpdate(task.RawText, translatedText);
                    }
                    else
                    {
                        TranslationManager.AddAndSaveTranslation(task.RawText, translatedText);
                        LiveUpdater.PushUpdate(task.RawText, translatedText);
                    }
                }
            }

            _isTranslating = false;
        }
    }
}