// Features/AutoTranslator.cs (Sistem antrean penerjemahan otomatis di latar belakang)
using System.Collections.Generic;
using System.Threading.Tasks;
using AlaskaGoldFeverTranslator.Managers;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class AutoTranslator
    {
        // Antrean teks yang menunggu untuk diterjemahkan
#pragma warning disable
        private static Queue<string> _translationQueue = new Queue<string>();

        // Status apakah mesin sedang bekerja agar tidak terjadi double-task
        private static bool _isTranslating = false;

        public static void Initialize()
        {
            Main.Logger.LogInfo("Auto Translator queue system initialized.");
        }

        // Method ini dipanggil oleh TextDumper saat menemukan teks baru
        public static void AddToQueue(string originalText)
        {
            // Jangan terjemahkan ulang jika teks sudah ada di kamus memori
            if (TranslationManager.TranslatedStrings.ContainsKey(originalText)) return;

            // Jangan masukkan jika teks sudah ada di dalam antrean
            if (_translationQueue.Contains(originalText)) return;

            _translationQueue.Enqueue(originalText);
            Main.Logger.LogInfo($"[AutoTranslator] Added to queue: \"{originalText}\"");

            // Mulai mesin penerjemah jika belum menyala
            if (!_isTranslating)
            {
                Task.Run(ProcessQueueAsync);
            }
        }

        // Proses pekerja latar belakang (Worker Thread)
        private static async Task ProcessQueueAsync()
        {
            _isTranslating = true;

            // Terus bekerja sampai antrean kosong
            while (_translationQueue.Count > 0)
            {
                string textToTranslate = _translationQueue.Dequeue();

                // [PENTING] Jeda 2 detik untuk menghindari IP diblokir (Error 429 Too Many Requests) oleh Google!
                await Task.Delay(2000);

                string translatedText = await TranslatorEngine.GoogleTranslate.TranslateAsync(textToTranslate, "en", "id");

                if (!string.IsNullOrEmpty(translatedText))
                {
                    Main.Logger.LogInfo($"[AutoTranslator] Success: \"{textToTranslate}\" -> \"{translatedText}\"");

                    // 1. Masukkan hasil ke dalam memori dan simpan ke file JSON
                    TranslationManager.AddAndSaveTranslation(textToTranslate, translatedText);

                    // 2. [FITUR BARU] Kirim perintah ke Main Thread untuk langsung mengubah teks di layar detik ini juga!
                    LiveUpdater.PushUpdate(textToTranslate, translatedText);
                }
            }

            _isTranslating = false;
        }
    }
}