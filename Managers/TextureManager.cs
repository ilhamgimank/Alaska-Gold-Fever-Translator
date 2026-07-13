// Managers/TextureManager.cs (Fitur untuk memuat dan menyimpan file gambar/tekstur)
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace AlaskaGoldFeverTranslator.Managers
{
    public static class TextureManager
    {
#pragma warning disable
        private static Dictionary<string, Sprite> _translatedSprites = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        // [FITUR BARU] Menyimpan Texture murni untuk RawImage (Kompas)
        private static Dictionary<string, Texture2D> _translatedRawTextures = new Dictionary<string, Texture2D>(System.StringComparer.OrdinalIgnoreCase);

        private static HashSet<string> _dumpedTextures = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            LoadTranslatedTextures();
            Main.Logger.LogInfo("Texture Manager initialized.");
        }

        public static void LoadTranslatedTextures()
        {
            _translatedSprites.Clear();
            _translatedRawTextures.Clear();
            string texPath = Path.Combine(FileManager.LocalizationPath, TranslationManager.CurrentLanguage, "Textures");

            if (Directory.Exists(texPath))
            {
                string[] files = Directory.GetFiles(texPath, "*.png");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    LoadTextureFromFile(file, fileName);
                }
                Main.Logger.LogInfo($"Loaded {_translatedSprites.Count} custom textures (Sprites & Raw) for language: {TranslationManager.CurrentLanguage}.");
            }
        }

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
            }
            catch (System.Exception ex) { Main.Logger.LogError("[Bypass] EncodeToPNG Error: " + ex.Message); }
            return null;
        }

        private static void LoadTextureFromFile(string path, string fileName)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (InvokeLoadImage(tex, fileData))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.name = fileName;

                    // Simpan sebagai Raw Texture (Untuk RawImage Kompas)
                    _translatedRawTextures[fileName] = tex;

                    // Simpan sebagai Sprite (Untuk Image standar)
                    Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = fileName;
                    _translatedSprites[fileName] = sprite;
                }
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TextureManager] Error loading PNG {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        // [FITUR BARU] Pembersih karakter ilegal tingkat DEWA menggunakan Regex!
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string clean = name.Replace("(Clone)", "");

            // Sapu bersih! HANYA boleh Huruf, Angka, Spasi, Strip (-), dan Garis Bawah (_)
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"[^a-zA-Z0-9\-_ ]", "_");

            return clean.Trim();
        }

        public static bool TryGetTranslatedSprite(string spriteName, out Sprite translatedSprite)
        {
            translatedSprite = null;
            if (string.IsNullOrEmpty(spriteName)) return false;
            return _translatedSprites.TryGetValue(SanitizeName(spriteName), out translatedSprite);
        }

        public static bool TryGetTranslatedTexture(string texName, out Texture translatedTex)
        {
            translatedTex = null;
            if (string.IsNullOrEmpty(texName)) return false;
            if (_translatedRawTextures.TryGetValue(SanitizeName(texName), out Texture2D t2d))
            {
                translatedTex = t2d;
                return true;
            }
            return false;
        }

        public static void DumpSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            DumpTextureInternal(sprite.texture, sprite.name);
        }

        public static void DumpRawTexture(Texture texture)
        {
            if (texture == null) return;
            DumpTextureInternal(texture, texture.name);
        }

        private static void DumpTextureInternal(Texture texture, string objectName)
        {
            // [SISTEM ANTI-CRASH LEVEL DEWA]
            // JANGAN menyentuh RenderTexture, Texture3D, atau Cubemap. Paksa mundur!
            if (!(texture is Texture2D)) return;

            // Cegah gambar berukuran tidak masuk akal (0x0) atau terlalu raksasa
            if (texture.width <= 0 || texture.height <= 0 || texture.width > 8192 || texture.height > 8192) return;

            // Bersihkan nama
            string cleanName = SanitizeName(objectName);

            // Blokir gambar-gambar sistem internal Unity (termasuk font yang sering bikin crash)
            if (string.IsNullOrEmpty(cleanName) || cleanName.Replace("_", "").Trim() == "" ||
                cleanName == "Background" || cleanName == "UISprite" || cleanName == "Knob" ||
                cleanName == "UIMask" || cleanName == "Checkmark" || cleanName == "DropdownArrow" ||
                cleanName.StartsWith("Unity") || cleanName.StartsWith("Font") || cleanName.StartsWith("Arial"))
                return;

            if (_dumpedTextures.Contains(cleanName)) return;
            _dumpedTextures.Add(cleanName);

            string outPath = Path.Combine(FileManager.DefaultTexturesPath, $"{cleanName}.png");
            if (File.Exists(outPath)) return;

            try
            {
                RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, tmp);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D myTexture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                byte[] bytes = InvokeEncodeToPNG(myTexture2D);
                if (bytes != null)
                {
                    File.WriteAllBytes(outPath, bytes);
                    Main.Logger.LogInfo($"[Texture Dumped] \"{cleanName}.png\" saved to [Default Textures]");
                }
                Object.Destroy(myTexture2D);
            }
            catch (System.Exception ex)
            {
                Main.Logger.LogError($"[TextureDumper] Failed to dump {cleanName}: {ex.Message}");
            }
        }
    }
}