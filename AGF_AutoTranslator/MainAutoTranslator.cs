using BepInEx;
using AlaskaGoldFeverTranslator.Features; // Untuk mengakses kodingan Auto Translator

namespace AlaskaGoldFeverTranslator.Modules
{
    [BepInPlugin("com.ilham.agfautotranslator", "AGF Auto Translator Module", "1.0.0")]
    [BepInDependency("com.ilham.alaskatranslator", BepInDependency.DependencyFlags.HardDependency)]
    public class MainAutoTranslator : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Module AGF Auto Translator v1.0.0 is loaded (Internet Required)!");

            // 1. Menyalakan mesin auto translator
            AutoTranslator.Initialize();

            // 2. [MODULAR LINK] Menyambungkan kabel pendengar ke Dumper Utama!
            // Setiap kali Dumper utama berteriak, fungsi AddToQueue akan otomatis dipanggil
            TextDumper.OnTextDumped += AutoTranslator.AddToQueue;
        }
    }
}