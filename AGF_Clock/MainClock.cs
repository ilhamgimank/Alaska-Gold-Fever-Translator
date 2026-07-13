using BepInEx;
using AlaskaGoldFeverTranslator.Features; // Untuk mengakses kodingan Jam

namespace AlaskaGoldFeverTranslator.Modules
{
    // [UPDATE] GUID khusus untuk Modul Jam
    [BepInPlugin("com.ilhamgimank.agfclock", "AGF Clock Module", "1.0.0")]
    // [UPDATE] Mewajibkan mod utama dengan nama GUID baru jalan duluan
    [BepInDependency("com.ilhamgimank.agftranslator", BepInDependency.DependencyFlags.HardDependency)]
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