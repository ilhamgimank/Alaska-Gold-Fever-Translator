// Features/DigitalClock.cs
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class DigitalClock
    {
        private static GameObject _clockObj;
        private static Text _localTimeText;
        public static Text InGameTimeText { get; private set; } // Terbuka ke publik agar nanti bisa diupdate dari luar

        public static void Initialize()
        {
            _clockObj = new GameObject("Alaska_DigitalClock");
            UnityEngine.Object.DontDestroyOnLoad(_clockObj);

            Canvas canvas = _clockObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = _clockObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ==========================================
            // 1. BACKGROUND UTAMA
            // ==========================================
            GameObject bgObj = new GameObject("ClockBackground");
            bgObj.transform.SetParent(_clockObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.85f); // Hitam gelap dengan 85% transparan

            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = new Vector2(0, -420); // Posisi di bawah tengah
            bgRt.sizeDelta = new Vector2(300, 60); // Persegi panjang untuk 2 kolom

            // ==========================================
            // 2. PANEL KIRI (IN-GAME TIME)
            // ==========================================
            GameObject leftPanel = new GameObject("LeftPanel_InGame");
            leftPanel.transform.SetParent(bgObj.transform, false);
            RectTransform leftRt = leftPanel.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0); // Kiri bawah
            leftRt.anchorMax = new Vector2(0.5f, 1); // Sampai ke tengah layar background
            leftRt.sizeDelta = Vector2.zero;
            leftRt.anchoredPosition = Vector2.zero;

            // Label "IN-GAME"
            CreateTextUI(leftPanel.transform, "Label", "IN-GAME", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerCenter,
                         new Vector2(0, 0.5f), new Vector2(1, 1));

            // Teks Angka In-Game (Sementara --:--:--)
            InGameTimeText = CreateTextUI(leftPanel.transform, "Value", "--:--:--", 24, Color.white, TextAnchor.UpperCenter,
                                          new Vector2(0, 0), new Vector2(1, 0.5f));

            // ==========================================
            // 3. PANEL KANAN (LOCAL TIME)
            // ==========================================
            GameObject rightPanel = new GameObject("RightPanel_Local");
            rightPanel.transform.SetParent(bgObj.transform, false);
            RectTransform rightRt = rightPanel.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0); // Mulai dari tengah
            rightRt.anchorMax = new Vector2(1, 1); // Sampai ujung kanan
            rightRt.sizeDelta = Vector2.zero;
            rightRt.anchoredPosition = Vector2.zero;

            // Label "LOCAL"
            CreateTextUI(rightPanel.transform, "Label", "LOCAL", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerCenter,
                         new Vector2(0, 0.5f), new Vector2(1, 1));

            // Teks Angka Local Time
            _localTimeText = CreateTextUI(rightPanel.transform, "Value", "00:00:00", 24, Color.white, TextAnchor.UpperCenter,
                                          new Vector2(0, 0), new Vector2(1, 0.5f));

            _clockObj.AddComponent<DigitalClockHandler>();
            Main.Logger.LogInfo("Dual Digital Clock UI injected successfully.");
        }

        // Fungsi bantuan (Helper) untuk membuat komponen Teks dengan cepat dan rapi
        private static Text CreateTextUI(Transform parent, string name, string textContent, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Text txt = obj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = textContent;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;

            // Menambahkan garis tepi (Outline) hitam agar teks lebih tajam
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            RectTransform rt = txt.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero; // Biarkan ukurannya otomatis mengikuti anchor (Full stretch)
            rt.anchoredPosition = Vector2.zero;

            return txt;
        }

        public static void UpdateLocalTime()
        {
            if (_localTimeText != null)
            {
                _localTimeText.text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public static void ToggleClock()
        {
#pragma warning disable
            if (_clockObj != null)
            {
                _clockObj.SetActive(!_clockObj.activeSelf);
            }
        }
    }

    public class DigitalClockHandler : MonoBehaviour
    {
        void Update()
        {
            DigitalClock.UpdateLocalTime();

            if (Input.GetKeyDown(KeyCode.F10))
            {
                DigitalClock.ToggleClock();
            }
        }
    }
}