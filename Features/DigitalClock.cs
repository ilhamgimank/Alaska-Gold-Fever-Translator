// Features/DigitalClock.cs (Modul khusus UI Jam Digital)
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class DigitalClock
    {
        private static GameObject _clockObj;
        private static Text _localTimeText;
        private static Text _inGameTimeText;

        public static void Initialize()
        {
            _clockObj = new GameObject("Alaska_UI_DigitalClock");
            UnityEngine.Object.DontDestroyOnLoad(_clockObj);
            _clockObj.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;

            Canvas canvas = _clockObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = _clockObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // BACKGROUND
            GameObject bgObj = new GameObject("ClockBackground");
            bgObj.transform.SetParent(_clockObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 1f);
            bgRt.anchorMax = new Vector2(0.5f, 1f);
            bgRt.pivot = new Vector2(0.5f, 1f);
            bgRt.anchoredPosition = new Vector2(0, -60);
            bgRt.sizeDelta = new Vector2(280, 60);

            // PANEL KIRI (IN-GAME)
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(bgObj.transform, false);
            RectTransform leftRt = leftPanel.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0); leftRt.anchorMax = new Vector2(0.5f, 1);
            leftRt.sizeDelta = Vector2.zero; leftRt.anchoredPosition = Vector2.zero;

            CreateTextUI(leftPanel.transform, "Label", "DALAM GAME", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerCenter, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero);
            _inGameTimeText = CreateTextUI(leftPanel.transform, "Value", "--:--:--", 24, Color.white, TextAnchor.UpperCenter, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

            // PANEL KANAN (LOCAL)
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(bgObj.transform, false);
            RectTransform rightRt = rightPanel.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0); rightRt.anchorMax = new Vector2(1, 1);
            rightRt.sizeDelta = Vector2.zero; rightRt.anchoredPosition = Vector2.zero;

            CreateTextUI(rightPanel.transform, "Label", "LOKAL", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerCenter, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero);
            _localTimeText = CreateTextUI(rightPanel.transform, "Value", "00:00:00", 24, Color.white, TextAnchor.UpperCenter, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

            _clockObj.AddComponent<DigitalClockHandler>();
            Main.Logger.LogInfo("Digital Clock Module ready (Press Home to toggle).");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#pragma warning disable
            if (_clockObj != null)
                _clockObj.SetActive(scene.buildIndex != 0 && !scene.name.ToLower().Contains("menu"));
        }

        public static void UpdateUI()
        {
            if (!_clockObj.activeSelf) return;

            // Mengambil data dari GameTimeManager
            if (GameTimeManager.IsResolved)
                _inGameTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", GameTimeManager.Hours, GameTimeManager.Minutes, GameTimeManager.Seconds);
            else
                _inGameTimeText.text = "--:--:--";

            _localTimeText.text = DateTime.Now.ToString("HH:mm:ss");
        }

        public static void Toggle()
        {
            if (_clockObj != null) _clockObj.SetActive(!_clockObj.activeSelf);
        }

        public static void Hide()
        {
            if (_clockObj != null) _clockObj.SetActive(false);
        }

        private static Text CreateTextUI(Transform parent, string name, string textContent, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = textContent; txt.fontSize = fontSize; txt.color = color; txt.alignment = alignment;
            Outline outline = obj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(1, -1);
            RectTransform rt = txt.rectTransform; rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = anchoredPos;
            return txt;
        }
    }

    public class DigitalClockHandler : MonoBehaviour
    {
        void Update()
        {
            DigitalClock.UpdateUI();
            if (Input.GetKeyDown(KeyCode.Home))
            {
                AnalogClock.Hide(); // Sembunyikan yang Analog jika yang Digital dinyalakan
                DigitalClock.Toggle();
            }
        }
    }
}