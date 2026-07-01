// Managers/TextureManager.cs (Fitur untuk memuat dan menyimpan file gambar/tekstur)
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class TextureManager
    {
        // Cache untuk menyimpan sprite/gambar bahasa Indonesia yang sudah diload ke RAM
#pragma warning disable
        private static Dictionary<string, Sprite> _translatedSprites = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        // Memori pencegah spam dump agar tidak membuat game lemot
        private static HashSet<string> _dumpedTextures = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            LoadTranslatedTextures();
            Main.Logger.LogInfo("Texture Manager initialized.");
        }

        // Memuat semua gambar .png dari folder Localization/.../Textures
        public static void LoadTranslatedTextures()
        {
            _translatedSprites.Clear();
            string texPath = Path.Combine(FileManager.LocalizationPath, TranslationManager.CurrentLanguage, "Textures");

            if (Directory.Exists(texPath))
            {
                string[] files = Directory.GetFiles(texPath, "*.png");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Sprite customSprite = LoadSpriteFromFile(file);

                    if (customSprite != null)
                    {
                        // Set nama sprite agar sama dengan aslinya
                        customSprite.name = fileName;
                        _translatedSprites[fileName] = customSprite;
                    }
                }
                Main.Logger.LogInfo($"Loaded {_translatedSprites.Count} custom textures for language: {TranslationManager.CurrentLanguage}.");
            }
        }

        // ====================================================================================
        // TRIK BYPASS JEDI (REFLECTION) v0.2.6
        // Mengeksekusi LoadImage dan EncodeToPNG secara dinamis dari memori Unity 
        // menggunakan HarmonyLib.AccessTools agar 100% akurat menemukan classnya!
        // ====================================================================================
        private static bool InvokeLoadImage(Texture2D tex, byte[] data)
        {
            try
            {
                var type = HarmonyLib.AccessTools.TypeByName("UnityEngine.ImageConversion");
                if (type != null)
                {
                    var method = HarmonyLib.AccessTools.Method(type, "LoadImage", new System.Type[] { typeof(Texture2D), typeof(byte[]) });
                    if (method != null) return (bool)method.Invoke(null, new object[] { tex, data });
                }
                else
                {
                    Main.Logger.LogError("[Bypass] Failed to find UnityEngine.ImageConversion class!");
                }
            }
            catch (System.Exception ex) { Main.Logger.LogError("[Bypass] LoadImage Error: " + ex.Message); }
            return false;
        }

        private static byte[] InvokeEncodeToPNG(Texture2D tex)
        {
            try
            {
                var type = HarmonyLib.AccessTools.TypeByName("UnityEngine.ImageConversion");
                if (type != null)
                {
                    var method = HarmonyLib.AccessTools.Method(type, "EncodeToPNG", new System.Type[] { typeof(Texture2D) });
                    if (method != null) return (byte[])method.Invoke(null, new object[] { tex });
                }
                else
                {
                    Main.Logger.LogError("[Bypass] Failed to find UnityEngine.ImageConversion class!");
                }
            }
            catch (System.Exception ex) { Main.Logger.LogError("[Bypass] EncodeToPNG Error: " + ex.Message); }
            return null;
        }
        // ====================================================================================

        // Fungsi magis untuk mengubah file .png lokal di harddisk menjadi Objek Sprite Unity
        private static Sprite LoadSpriteFromFile(string path)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                // Membuat kanvas tekstur kosong
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                // Menggunakan fungsi Bypass untuk LoadImage
                if (InvokeLoadImage(tex, fileData))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;

                    // Mengubah Texture2D menjadi Sprite UI (Pivot di tengah 0.5, 0.5)
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TextureManager] Error loading PNG {Path.GetFileName(path)}: {ex.Message}");
            }
            return null;
        }

        // Cek apakah gambar yang dirender game punya versi Indonesianya
        public static bool TryGetTranslatedSprite(string spriteName, out Sprite translatedSprite)
        {
            translatedSprite = null;
            if (string.IsNullOrEmpty(spriteName)) return false;

            // Membersihkan nama dari embel-embel cloning bawaan engine
            string cleanName = spriteName.Replace("(Clone)", "").Trim();
            return _translatedSprites.TryGetValue(cleanName, out translatedSprite);
        }

        // Fungsi Dumper: Menyalin tekstur dari GPU ke Harddisk
        public static void DumpTexture(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            string cleanName = sprite.name.Replace("(Clone)", "").Trim();

            // Memblokir tekstur UI polosan bawaan engine agar folder dump tidak kotor
            if (cleanName == "Background" || cleanName == "UISprite" || cleanName == "Knob" ||
                cleanName == "UIMask" || cleanName == "Checkmark" || cleanName == "DropdownArrow" || cleanName.StartsWith("Unity"))
                return;

            // Jangan buang waktu jika gambar sudah ada di memori
            if (_dumpedTextures.Contains(cleanName)) return;
            _dumpedTextures.Add(cleanName);

            string outPath = Path.Combine(FileManager.DefaultTexturesPath, $"{cleanName}.png");
            if (File.Exists(outPath)) return;

            // TRIK BYPASS TINGKAT TINGGI: Meng-copy tekstur via RenderTexture GPU
            // (Karena kebanyakan gambar aslinya disetting "Not Readable" oleh developer game)
            try
            {
                // [PERBAIKAN] Gunakan ARGB32 agar background transparan (Alpha) pada gambar UI tidak berubah jadi hitam!
                RenderTexture tmp = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(sprite.texture, tmp);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                // [PERBAIKAN] Format Texture2D juga harus mendukung transparansi (RGBA32)
                Texture2D myTexture2D = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                // Ubah menjadi PNG biner menggunakan Bypass
                byte[] bytes = InvokeEncodeToPNG(myTexture2D);

                if (bytes != null)
                {
                    File.WriteAllBytes(outPath, bytes);
                    Main.Logger.LogInfo($"[Texture Dumped] \"{cleanName}.png\" saved to [Default Textures]");
                }

                Object.Destroy(myTexture2D); // Cegah Memory Leak
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TextureDumper] Failed to dump {cleanName}: {ex.Message}");
            }
        }
    }
}