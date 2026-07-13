using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using AlaskaGoldFeverTranslator;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features.TranslatorEngine;

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

#pragma warning disable
        private static Queue<TranslationTask> _translationQueue = new Queue<TranslationTask>();
        private static bool _isTranslating = false;

        // Variabel untuk melacak progress log [1/20]
        private static int _totalTasksInQueue = 0;
        private static int _processedTasksCount = 0;

        // Pengaman Thread-Safe agar Main Thread dan Background Thread tidak tabrakan
        private static readonly object _queueLock = new object();

        // Enum untuk melacak mesin penerjemah yang aktif
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

        private static bool IsIndonesianOrRp(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            // Indonesian stopwords & kata-kata sangat umum hasil translasi
            string[] idWords = {
                "yang", "untuk", "dengan", "adalah", "bisa", "pada", "dari", "dalam",
                "akan", "sudah", "telah", "tidak", "bukan", "atau", "hanya", "jika",
                "bila", "saya", "anda", "kamu", "kita", "kami", "mereka", "emas",
                "tambang", "beliung", "uang", "pertanian", "membeli", "kumpulkan",
                "tukarkan", "memiliki", "cukup", "tunai", "mulai", "menambang", "lengkapi"
            };

            // Blokir mutlak jika teks mengandung simbol hasil konversi mata uang rupiah
            if (Regex.IsMatch(text, @"\b[Rr]p\b") || text.Contains("Rp.") || text.Contains("Rp ") || text.Contains("(Rp") || text.Contains("IDR"))
                return true;

            string clean = Regex.Replace(text.ToLower(), @"[^a-z\s]", " ");
            string[] tokens = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                foreach (var idWord in idWords)
                {
                    if (token == idWord) return true;
                }
            }
            return false;
        }

        public static void AddToQueue(string originalText, bool isRegex = false, string regexKey = null)
        {
            // Jangan masukkan jika teks sudah ada di dalam antrean memori
            if (TranslationManager.TranslatedStrings.ContainsKey(originalText)) return;

            // [DOUBLE GUARD] Cegah memasukkan teks yang sudah terjemahan Indonesia atau sudah dikonversi ke Rp
            if (IsIndonesianOrRp(originalText))
            {
                Main.Logger.LogWarning($"[AutoTranslator] 🛡️ BLOCKED Indonesian/Rp text from entering queue: \"{originalText}\"");
                return;
            }

            // Menggunakan lock agar aman jika banyak teks masuk sekaligus
            lock (_queueLock)
            {
                _translationQueue.Enqueue(new TranslationTask { RawText = originalText, IsRegex = isRegex, RegexKey = regexKey });
                _totalTasksInQueue++; // Tambah total antrean

                Main.Logger.LogInfo($"[AutoTranslator] 📥 Added to queue (Total Pending: {_totalTasksInQueue}): \"{originalText}\"");

                if (!_isTranslating)
                {
                    Task.Run(ProcessQueueAsync);
                }
            }
        }

        private static async Task ProcessQueueAsync()
        {
            _isTranslating = true;
            Main.Logger.LogInfo($"[AutoTranslator] 🚀 Starting Queue Translation: {_totalTasksInQueue} text(s) pending.");

            while (true)
            {
                TranslationTask task;
                int currentIndex;
                int currentTotal;

                // Ambil tugas dengan aman menggunakan Lock
                lock (_queueLock)
                {
                    if (_translationQueue.Count == 0)
                    {
                        if (_totalTasksInQueue > 0)
                        {
                            Main.Logger.LogInfo($"[AutoTranslator] ✅ All {_totalTasksInQueue} text(s) have been successfully translated!");
                        }

                        // Reset variabel antrean
                        _totalTasksInQueue = 0;
                        _processedTasksCount = 0;
                        _isTranslating = false;
                        break; // Keluar dari loop
                    }

                    task = _translationQueue.Dequeue();
                    _processedTasksCount++;

                    // Simpan variabel lokal untuk kebutuhan log di luar lock
                    currentIndex = _processedTasksCount;
                    currentTotal = _totalTasksInQueue;
                }

                // Jeda 2 detik untuk menghindari IP diblokir API
                await Task.Delay(2000);

                // --- SMART TAG MASKER (Anti-Terjemah Kode Warna & Sprite) ---
                List<string> protectedTags = new List<string>();
                string textToTranslate = Regex.Replace(task.RawText, @"<[^>]+>", match =>
                {
                    protectedTags.Add(match.Value);
                    char letter = (char)('A' + (protectedTags.Count - 1)); // Menjadi A, B, C, dst.
                    return $"__TAG_{letter}__";
                });

                string translatedText = null;

                // Mengeksekusi mesin terjemahan sesuai dengan pilihan yang sedang aktif
                if (ActiveEngine == TranslationEngine.Google)
                {
                    translatedText = await GoogleTranslate.TranslateAsync(textToTranslate, "en", "id");
                }
                else if (ActiveEngine == TranslationEngine.MyMemory)
                {
                    translatedText = await MyMemoryTranslate.TranslateAsync(textToTranslate, "en", "id");
                }

                if (!string.IsNullOrEmpty(translatedText))
                {
                    // Lakukan filter jika ternyata hasil terjemahannya sama persis, kosong, atau mengandung Rp
                    if (translatedText.Trim().Equals(task.RawText.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        Main.Logger.LogWarning($"[AutoTranslator] [{currentIndex}/{currentTotal}] ⚠️ Skipped (Identical Result): \"{task.RawText}\"");
                        continue;
                    }

                    if (IsIndonesianOrRp(translatedText))
                    {
                        // Jika hasil terjemahan secara tidak sengaja mengembalikan Rp dari API (sangat jarang terjadi)
                        Main.Logger.LogWarning($"[AutoTranslator] [{currentIndex}/{currentTotal}] ⚠️ Skipped (Result has Rp): \"{translatedText}\"");
                        continue;
                    }

                    if (task.IsRegex)
                    {
                        // Menyulap angka murni menjadi parameter {0}, {1}
                        int counter = 0;
                        translatedText = Regex.Replace(translatedText, @"\d+", match => "{" + (counter++) + "}");
                    }

                    // --- UNMASK: Mengembalikan Tag aslinya ke dalam teks ---
                    for (int i = 0; i < protectedTags.Count; i++)
                    {
                        char letter = (char)('A' + i);
                        translatedText = Regex.Replace(translatedText, $@"__\s*TAG\s*_\s*{letter}\s*__", protectedTags[i], RegexOptions.IgnoreCase);
                    }

                    if (task.IsRegex)
                    {
                        TranslationManager.AddAndSaveRegexTranslation(task.RegexKey, translatedText);
                        Main.Logger.LogInfo($"[AutoTranslator] [{currentIndex}/{currentTotal}] ⚙️ Regex Saved: \"{task.RegexKey}\" -> \"{translatedText}\"");
                    }
                    else
                    {
                        TranslationManager.AddAndSaveTranslation(task.RawText, translatedText);
                        LiveUpdater.PushUpdate(task.RawText, translatedText);
                        Main.Logger.LogInfo($"[AutoTranslator] [{currentIndex}/{currentTotal}] ✨ Static Success: \"{task.RawText}\" -> \"{translatedText}\"");
                    }
                }
            }
        }
    }

    // Handler untuk menangkap kombinasi tombol ganti mesin
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