using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using AlaskaGoldFeverTranslator.Managers;
using AlaskaGoldFeverTranslator.Features.TranslatorEngine;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class AutoTranslator
    {
        private struct TranslationTask
        {
            public string RawText;
            public bool IsRegex;
            public string RegexKey;
        }

#pragma warning disable
        private static Queue<TranslationTask> _translationQueue = new Queue<TranslationTask>();
        private static bool _isTranslating = false;

        private static int _totalTasksInQueue = 0;
        private static int _processedTasksCount = 0;
        private static readonly object _queueLock = new object();

        public static void Initialize()
        {
            GameObject handlerObj = new GameObject("Alaska_AutoTranslatorHandler");
            UnityEngine.Object.DontDestroyOnLoad(handlerObj);
            handlerObj.AddComponent<AutoTranslatorHandler>();

            Main.Logger.LogInfo($"Auto Translator queue system initialized. Active Engine: {ConfigManager.ActiveEngine.Value}");
        }

        public static void AddToQueue(string originalText, bool isRegex = false, string regexKey = null)
        {
            if (TranslationManager.TranslatedStrings.ContainsKey(originalText)) return;

            lock (_queueLock)
            {
                _translationQueue.Enqueue(new TranslationTask { RawText = originalText, IsRegex = isRegex, RegexKey = regexKey });
                _totalTasksInQueue++;

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

                lock (_queueLock)
                {
                    if (_translationQueue.Count == 0)
                    {
                        if (_totalTasksInQueue > 0)
                        {
                            Main.Logger.LogInfo($"[AutoTranslator] ✅ All {_totalTasksInQueue} text(s) have been successfully translated!");
                        }

                        _totalTasksInQueue = 0;
                        _processedTasksCount = 0;
                        _isTranslating = false;
                        break;
                    }

                    task = _translationQueue.Dequeue();
                    _processedTasksCount++;

                    currentIndex = _processedTasksCount;
                    currentTotal = _totalTasksInQueue;
                }

                await Task.Delay(2000);

                List<string> protectedTags = new List<string>();
                string textToTranslate = Regex.Replace(task.RawText, @"<[^>]+>", match =>
                {
                    protectedTags.Add(match.Value);
                    char letter = (char)('A' + (protectedTags.Count - 1));
                    return $"__TAG_{letter}__";
                });

                string translatedText = null;

                if (ConfigManager.ActiveEngine.Value == TranslatorEngineType.Google)
                {
                    translatedText = await GoogleTranslate.TranslateAsync(textToTranslate, "en", "id");
                }
                else if (ConfigManager.ActiveEngine.Value == TranslatorEngineType.MyMemory)
                {
                    translatedText = await MyMemoryTranslate.TranslateAsync(textToTranslate, "en", "id");
                }

                if (!string.IsNullOrEmpty(translatedText))
                {
                    if (translatedText.Trim().Equals(task.RawText.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        Main.Logger.LogWarning($"[AutoTranslator] [{currentIndex}/{currentTotal}] ⚠️ Skipped (Identical Result): \"{task.RawText}\"");
                        continue;
                    }

                    if (task.IsRegex)
                    {
                        int counter = 0;
                        translatedText = Regex.Replace(translatedText, @"\d+", match => "{" + (counter++) + "}");
                    }

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

    public class AutoTranslatorHandler : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.T))
            {
                ConfigManager.ActiveEngine.Value = ConfigManager.ActiveEngine.Value == TranslatorEngineType.Google
                    ? TranslatorEngineType.MyMemory
                    : TranslatorEngineType.Google;

                // Menyimpan pilihan mesin secara permanen ke file config!
                ConfigManager.Save();

                Main.Logger.LogInfo($"[AutoTranslator] 🔄 Translation Engine switched to: {ConfigManager.ActiveEngine.Value}");
            }
        }
    }
}