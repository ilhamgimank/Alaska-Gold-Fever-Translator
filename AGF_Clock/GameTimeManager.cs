// Features/GameTimeManager.cs (Manajer pusat penyedot waktu In-Game)
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class GameTimeManager
    {
        public static bool IsResolved { get; private set; }
        public static int Hours { get; private set; }
        public static int Minutes { get; private set; }
        public static int Seconds { get; private set; }

        public static bool IsInitialized { get; private set; } // Mencegah inisialisasi ganda

        private static Type _timeOfDayControllerType;
        private static object _timeOfDayInstance;
        private static PropertyInfo _timeProp;
        private static PropertyInfo _floatTimeProp;
        private static float _resolveTimer = 0f;

        public static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            GameObject obj = new GameObject("Alaska_GameTimeManager");
            UnityEngine.Object.DontDestroyOnLoad(obj);
            obj.AddComponent<GameTimeManagerHandler>();

            SceneManager.sceneLoaded += OnSceneLoaded;
            Main.Logger.LogInfo("Game Time Manager initialized (Core Engine Hacker).");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 0 || scene.name.ToLower().Contains("menu"))
            {
                IsResolved = false;
                Hours = 0; Minutes = 0; Seconds = 0;
            }
        }

        private static void TryResolve()
        {
            if (IsResolved) return;
            try
            {
                if (_timeOfDayControllerType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        _timeOfDayControllerType = assembly.GetType("TimeOfDayController") ?? assembly.GetType("BakedGames.Alaska.TimeOfDayController");
                        if (_timeOfDayControllerType != null) break;
                    }
                }

                if (_timeOfDayControllerType != null)
                {
                    var instanceField = _timeOfDayControllerType.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
#pragma warning disable
                    object instance = (instanceField != null) ? instanceField.GetValue(null) : null;

                    if (instance != null)
                    {
                        object timeOfDayObj = null;
                        var todProp = _timeOfDayControllerType.GetProperty("TimeOfDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        if (todProp != null) timeOfDayObj = todProp.GetValue(instance);

                        if (timeOfDayObj == null)
                        {
                            var todField = _timeOfDayControllerType.GetField("_timeOfDay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                            if (todField != null) timeOfDayObj = todField.GetValue(instance);
                        }

                        if (timeOfDayObj != null)
                        {
                            _timeOfDayInstance = timeOfDayObj;
                            Type todType = timeOfDayObj.GetType();

                            _timeProp = todType.GetProperty("Time", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                            _floatTimeProp = todType.GetProperty("FloatTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                            if (_timeProp != null || _floatTimeProp != null)
                            {
                                IsResolved = true;
                                Main.Logger.LogInfo("[GameTimeManager] In-Game Time Data HACKED & Synced! ✓");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void UpdateTime()
        {
            if (!IsResolved)
            {
                _resolveTimer += Time.deltaTime;
                if (_resolveTimer > 2f)
                {
                    _resolveTimer = 0f;
                    TryResolve();
                }
                return;
            }

            try
            {
                if (_timeProp != null)
                {
                    TimeSpan ts = (TimeSpan)_timeProp.GetValue(_timeOfDayInstance);
                    Hours = ts.Hours;
                    Minutes = ts.Minutes;
                    Seconds = ts.Seconds;
                }
                else if (_floatTimeProp != null)
                {
                    float floatTime = (float)_floatTimeProp.GetValue(_timeOfDayInstance);
                    int totalSeconds = (int)(floatTime * 24f * 3600f);
                    Hours = (totalSeconds / 3600) % 24;
                    Minutes = (totalSeconds / 60) % 60;
                    Seconds = totalSeconds % 60;
                }
            }
            catch
            {
                IsResolved = false;
            }
        }
    }

    public class GameTimeManagerHandler : MonoBehaviour
    {
        void Update()
        {
            GameTimeManager.UpdateTime();
        }
    }
}