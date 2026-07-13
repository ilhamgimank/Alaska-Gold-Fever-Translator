using BepInEx;
using AlaskaGoldFeverTranslator.Features; // Untuk mengakses kodingan Jam

namespace AlaskaGoldFeverTranslator.Modules
{
    [BepInPlugin("com.ilham.agfclock", "AGF Clock Module", "1.0.0")]
    // Ini kuncinya! Mewajibkan mod utama (Translator) jalan duluan
    [BepInDependency("com.ilham.alaskatranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class MainClock : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Module AGF Clock v1.0.0 is loaded!");

            // Menyalakan fitur jam dari modul ini
            GameTimeManager.Initialize();
            DigitalClock.Initialize();
            AnalogClock.Initialize();
        }
    }
}