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
            var imageSetter = AccessTools.PropertySetter(typeof(Image), "sprite");
            if (imageSetter != null) harmony.Patch(imageSetter, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(ImageSpritePrefix)));

            var imageOnEnable = AccessTools.Method(typeof(Image), "OnEnable");
            if (imageOnEnable != null) harmony.Patch(imageOnEnable, postfix: new HarmonyMethod(typeof(ImagePatch), nameof(ImageOnEnablePostfix)));

            var srSetter = AccessTools.PropertySetter(typeof(SpriteRenderer), "sprite");
            if (srSetter != null) harmony.Patch(srSetter, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(SpriteRendererPrefix)));

            var rawImageSetter = AccessTools.PropertySetter(typeof(RawImage), "texture");
            if (rawImageSetter != null) harmony.Patch(rawImageSetter, prefix: new HarmonyMethod(typeof(ImagePatch), nameof(RawImageTexturePrefix)));

            var rawImageOnEnable = AccessTools.Method(typeof(RawImage), "OnEnable");
            if (rawImageOnEnable != null) harmony.Patch(rawImageOnEnable, postfix: new HarmonyMethod(typeof(ImagePatch), nameof(RawImageOnEnablePostfix)));

            Main.Logger.LogInfo("Texture Replacer (Stable Version) patches applied successfully.");
        }

        private static void ImageSpritePrefix(ref Sprite value)
        {
            try
            {
                if (value == null) return;
                TextureManager.DumpSprite(value);
                if (TextureManager.TryGetTranslatedSprite(value.name, out Sprite translatedSprite)) value = translatedSprite;
            }
            catch { }
        }

        private static void ImageOnEnablePostfix(Image __instance)
        {
            try
            {
                if (__instance == null || __instance.sprite == null) return;
                TextureManager.DumpSprite(__instance.sprite);
                if (TextureManager.TryGetTranslatedSprite(__instance.sprite.name, out Sprite translatedSprite)) __instance.sprite = translatedSprite;
            }
            catch { }
        }

        private static void SpriteRendererPrefix(ref Sprite value)
        {
            try
            {
                if (value == null) return;
                TextureManager.DumpSprite(value);
                if (TextureManager.TryGetTranslatedSprite(value.name, out Sprite translatedSprite)) value = translatedSprite;
            }
            catch { }
        }

        private static void RawImageTexturePrefix(ref Texture value)
        {
            try
            {
                if (value == null) return;
                TextureManager.DumpRawTexture(value);
                if (TextureManager.TryGetTranslatedTexture(value.name, out Texture translatedTex)) value = translatedTex;
            }
            catch { }
        }

        private static void RawImageOnEnablePostfix(RawImage __instance)
        {
            try
            {
                if (__instance == null || __instance.texture == null) return;
                TextureManager.DumpRawTexture(__instance.texture);
                if (TextureManager.TryGetTranslatedTexture(__instance.texture.name, out Texture translatedTex)) __instance.texture = translatedTex;
            }
            catch { }
        }
    }
}