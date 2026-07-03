// Features/DigitalClock.cs
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class DigitalClock
    {
        private static GameObject _clockObj;
        private static Text _clockText;

        public static void Initialize()
        {
            _clockObj = new GameObject("Alaska_DigitalClock");

            // [FIX CS0104] Menambahkan "UnityEngine." secara spesifik 
            // agar tidak bentrok dengan System.Object bawaan C#
            UnityEngine.Object.DontDestroyOnLoad(_clockObj);

            Canvas canvas = _clockObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = _clockObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject textObj = new GameObject("TimeText");
            textObj.transform.SetParent(_clockObj.transform, false);

            _clockText = textObj.AddComponent<Text>();
            _clockText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _clockText.fontSize = 24;
            _clockText.color = Color.white;
            _clockText.alignment = TextAnchor.UpperRight;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            RectTransform rt = _clockText.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(200, 50);

            _clockObj.AddComponent<DigitalClockHandler>();

            Main.Logger.LogInfo("Digital Clock UI injected successfully.");
        }

        public static void UpdateTime()
        {
            // Pengecekan null standar Unity
            if (_clockText != null)
            {
                _clockText.text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public static void ToggleClock()
        {
            // Pengecekan null standar Unity
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
            DigitalClock.UpdateTime();

            if (Input.GetKeyDown(KeyCode.F10))
            {
                DigitalClock.ToggleClock();
            }
        }
    }
}