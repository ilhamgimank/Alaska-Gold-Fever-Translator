// Patches/ImagePatch.cs (Fitur untuk mencegat gambar yang diload ke layar)
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using AlaskaGoldFeverTranslator.Managers;

namespace AlaskaGoldFeverTranslator.Patches
{
    public static class ImagePatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            // 1. Patch ke Setingan Sprite (UGUI Image)
            var imageSetter = AccessTools.PropertySetter(typeof(Image), "sprite");
            if (imageSetter != null)
            {
                harmony.Patch(imageSetter, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(ImageSpritePrefix)));
            }

            // 2. Patch saat UI Gambar dinyalakan ke layar (Mengakali gambar Hardcoded di Prefab)
            var imageOnEnable = AccessTools.Method(typeof(Image), "OnEnable");
            if (imageOnEnable != null)
            {
                harmony.Patch(imageOnEnable, postfix: new HarmonyMethod(typeof(ImagePatch), nameof(ImageOnEnablePostfix)));
            }

            // 3. Patch ke SpriteRenderer (Untuk gambar dunia 2D/3D non-UI)
            var srSetter = AccessTools.PropertySetter(typeof(SpriteRenderer), "sprite");
            if (srSetter != null)
            {
                harmony.Patch(srSetter, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(SpriteRendererPrefix)));
            }

            Main.Logger.LogInfo("Texture Replacer & Dumper patches applied successfully.");
        }

        // Mengeksekusi Dumper dan Translasi SEBELUM gambar diset oleh game
        private static void ImageSpritePrefix(ref Sprite value)
        {
            if (value == null) return;

            // Merekam gambar aslinya (Bypass jika sudah tersimpan)
            TextureManager.DumpTexture(value);

            // Mengganti dengan gambar mod jika tersedia
            if (TextureManager.TryGetTranslatedSprite(value.name, out Sprite translatedSprite))
            {
                value = translatedSprite;
            }
        }

        // Mengeksekusi penyisipan pada gambar yang sudah menempel di layar dari awal
        private static void ImageOnEnablePostfix(Image __instance)
        {
            if (__instance == null || __instance.sprite == null) return;

            TextureManager.DumpTexture(__instance.sprite);

            if (TextureManager.TryGetTranslatedSprite(__instance.sprite.name, out Sprite translatedSprite))
            {
                __instance.sprite = translatedSprite;
            }
        }

        // Mengeksekusi penggantian Sprite pada objek dunia
        private static void SpriteRendererPrefix(ref Sprite value)
        {
            if (value == null) return;

            TextureManager.DumpTexture(value);

            if (TextureManager.TryGetTranslatedSprite(value.name, out Sprite translatedSprite))
            {
                value = translatedSprite;
            }
        }
    }
}