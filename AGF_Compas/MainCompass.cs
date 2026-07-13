using BepInEx;
using HarmonyLib;

namespace AlaskaGoldFeverTranslator.Modules
{
    // [UPDATE] GUID khusus untuk Modul Kompas
    [BepInPlugin("com.ilhamgimank.agfcompass", "AGF Compass Module", "1.0.0")]
    // [UPDATE] Mewajibkan mod utama dengan nama GUID baru jalan duluan
    [BepInDependency("com.ilhamgimank.agftranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class MainCompass : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Module AGF Compass v1.0.0 is loaded!");

            // Menyalakan patch Harmony khusus kompas secara mandiri
            Harmony.CreateAndPatchAll(typeof(CompassPatch));
        }
    }
}