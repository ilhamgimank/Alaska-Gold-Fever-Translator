using BepInEx;
using HarmonyLib;

namespace AlaskaGoldFeverTranslator.Modules
{
    [BepInPlugin("com.ilham.agfcompass", "AGF Compass Module", "1.0.0")]
    [BepInDependency("com.ilham.alaskatranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class MainCompass : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Module AGF Compass v1.0.0 is loaded!");

            // [UPDATE] Menyalakan patch Harmony khusus kompas secara mandiri!
            Harmony.CreateAndPatchAll(typeof(CompassPatch));
        }
    }
}