using BepInEx;
using AlaskaGoldFeverTranslator.Features; // Untuk mengakses kodingan Auto Translator

namespace AlaskaGoldFeverTranslator.Modules
{
    // [UPDATE] GUID khusus untuk Mesin Terjemahan Otomatis
    [BepInPlugin("com.ilhamgimank.agfautotranslator", "AGF Auto Translator Module", "1.0.0")]
    // [UPDATE] Mewajibkan mod utama dengan nama GUID baru jalan duluan
    [BepInDependency("com.ilhamgimank.agftranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class MainAutoTranslator : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Module AGF Auto Translator v1.0.0 is loaded (Internet Required)!");

            // 1. Menyalakan mesin auto translator
            AutoTranslator.Initialize();

            // 2. [MODULAR LINK] Menyambungkan kabel pendengar ke Dumper Utama!
            TextDumper.OnTextDumped += AutoTranslator.AddToQueue;
        }
    }
}