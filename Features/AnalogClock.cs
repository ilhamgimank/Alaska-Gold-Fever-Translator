// Features/AnalogClock.cs (Modul khusus UI Jam Analog mekanik)
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class AnalogClock
    {
        private static GameObject _clockObj;
        private static Canvas _canvas; // [FIX] Kita gunakan Canvas untuk hide/show
        private static Text _localTimeText;
        private static Text _inGameTimeText;
        private static RectTransform _hourHandRt;
        private static RectTransform _minuteHandRt;
        private static RectTransform _secondHandRt;

        public static void Initialize()
        {
            // [FIX] Otomatis menyalakan Otak Jam agar bebas error!
            GameTimeManager.Initialize();

            _clockObj = new GameObject("Alaska_UI_AnalogClock");
            UnityEngine.Object.DontDestroyOnLoad(_clockObj);
            SceneManager.sceneLoaded += OnSceneLoaded;

            _canvas = _clockObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            _canvas.enabled = false; // [FIX] Sembunyikan UI-nya saja, script tetap hidup!

            CanvasScaler scaler = _clockObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // BACKGROUND
            GameObject bgObj = new GameObject("ClockBackground");
            bgObj.transform.SetParent(_clockObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 1f); bgRt.anchorMax = new Vector2(0.5f, 1f); bgRt.pivot = new Vector2(0.5f, 1f);
            bgRt.anchoredPosition = new Vector2(0, -60); bgRt.sizeDelta = new Vector2(320, 110);

            // PANEL KIRI (ANALOG)
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(bgObj.transform, false);
            RectTransform leftRt = leftPanel.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0); leftRt.anchorMax = new Vector2(0.5f, 1);
            leftRt.sizeDelta = Vector2.zero; leftRt.anchoredPosition = Vector2.zero;

            CreateTextUI(leftPanel.transform, "Label", "DALAM GAME", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -5));

            // LINGKARAN JAM
            GameObject faceObj = new GameObject("AnalogFace");
            faceObj.transform.SetParent(leftPanel.transform, false);
            Image faceImg = faceObj.AddComponent<Image>();
            faceImg.sprite = Resources.GetBuiltinResource<Sprite>("Knob.png"); faceImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            Outline faceOutline = faceObj.AddComponent<Outline>(); faceOutline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f); faceOutline.effectDistance = new Vector2(1, -1);
            RectTransform faceRt = faceObj.GetComponent<RectTransform>();
            faceRt.anchorMin = new Vector2(0.5f, 0.5f); faceRt.anchorMax = new Vector2(0.5f, 0.5f); faceRt.pivot = new Vector2(0.5f, 0.5f);
            faceRt.sizeDelta = new Vector2(50, 50); faceRt.anchoredPosition = new Vector2(0, 0);

            // JARUM JAM
            _hourHandRt = CreateHandUI(faceObj.transform, "HourHand", new Vector2(3, 14), Color.white);
            _minuteHandRt = CreateHandUI(faceObj.transform, "MinuteHand", new Vector2(2, 22), new Color(0.8f, 0.8f, 0.8f));
            _secondHandRt = CreateHandUI(faceObj.transform, "SecondHand", new Vector2(1, 24), new Color(0.9f, 0.2f, 0.2f));

            // POROS TENGAH
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(faceObj.transform, false);
            Image dotImg = dotObj.AddComponent<Image>(); dotImg.sprite = Resources.GetBuiltinResource<Sprite>("Knob.png"); dotImg.color = Color.white;
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 0.5f); dotRt.anchorMax = new Vector2(0.5f, 0.5f); dotRt.sizeDelta = new Vector2(6, 6); dotRt.anchoredPosition = Vector2.zero;

            _inGameTimeText = CreateTextUI(leftPanel.transform, "Value", "--:--:--", 16, Color.white, TextAnchor.LowerCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 5));

            // PANEL KANAN (LOCAL)
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(bgObj.transform, false);
            RectTransform rightRt = rightPanel.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0); rightRt.anchorMax = new Vector2(1, 1);
            rightRt.sizeDelta = Vector2.zero; rightRt.anchoredPosition = Vector2.zero;

            CreateTextUI(rightPanel.transform, "Label", "LOKAL", 12, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -5));
            _localTimeText = CreateTextUI(rightPanel.transform, "Value", "00:00:00", 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -10));

            _clockObj.AddComponent<AnalogClockHandler>();
            Main.Logger.LogInfo("Analog Clock Module ready (Press End to toggle).");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_canvas != null) _canvas.enabled = false;
        }

        public static void UpdateUI()
        {
            if (_canvas == null || !_canvas.enabled) return;

            if (GameTimeManager.IsResolved)
            {
                int h = GameTimeManager.Hours, m = GameTimeManager.Minutes, s = GameTimeManager.Seconds;
                _inGameTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", h, m, s);

                if (_hourHandRt != null)
                {
                    _hourHandRt.localRotation = Quaternion.Euler(0, 0, -((h % 12) * 30f + (m / 60f) * 30f));
                    _minuteHandRt.localRotation = Quaternion.Euler(0, 0, -(m * 6f + (s / 60f) * 6f));
                    _secondHandRt.localRotation = Quaternion.Euler(0, 0, -(s * 6f));
                }
            }
            else
            {
                _inGameTimeText.text = "--:--:--";
            }

            _localTimeText.text = DateTime.Now.ToString("HH:mm:ss");
        }

        public static void Toggle()
        {
            if (_canvas != null) _canvas.enabled = !_canvas.enabled;
        }

        public static void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
        }

        private static RectTransform CreateHandUI(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject hand = new GameObject(name); hand.transform.SetParent(parent, false);
            Image img = hand.AddComponent<Image>(); img.color = color;
            RectTransform rt = hand.GetComponent<RectTransform>(); rt.sizeDelta = size;
            rt.pivot = new Vector2(0.5f, 0f); rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static Text CreateTextUI(Transform parent, string name, string textContent, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos)
        {
            GameObject obj = new GameObject(name); obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = textContent; txt.fontSize = fontSize; txt.color = color; txt.alignment = alignment;
            Outline outline = obj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(1, -1);
            RectTransform rt = txt.rectTransform; rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = anchoredPos;
            return txt;
        }
    }

    public class AnalogClockHandler : MonoBehaviour
    {
        void Update()
        {
            AnalogClock.UpdateUI();
            if (Input.GetKeyDown(KeyCode.End))
            {
                DigitalClock.Hide();
                AnalogClock.Toggle();
            }
        }
    }
}