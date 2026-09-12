using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle.Editor
{
    /// <summary>
    /// Keeps the authored room and decoration art mobile-friendly and consistent.
    /// Runtime code loads these assets as Texture2D and creates UI sprites on demand.
    /// </summary>
    internal sealed class CatMetaArtImporter : AssetPostprocessor
    {
        private const string RoomFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle/Art/Rooms/";
        private const string DecorationFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle/Art/Decorations/";
        private const string ThumbnailFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle/Art/RoomThumbnails/";

        private void OnPreprocessTexture()
        {
            bool isRoom = assetPath.StartsWith(RoomFolder, System.StringComparison.Ordinal);
            bool isDecoration = assetPath.StartsWith(DecorationFolder, System.StringComparison.Ordinal);
            bool isThumbnail = assetPath.StartsWith(ThumbnailFolder, System.StringComparison.Ordinal);
            if (!isRoom && !isDecoration && !isThumbnail)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = isRoom ? 2048 : isThumbnail ? 512 : 1024;
            importer.alphaSource = isDecoration
                ? TextureImporterAlphaSource.FromInput
                : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = isDecoration;

            ConfigureMobile(importer, "Android", isRoom ? 2048 : isThumbnail ? 512 : 1024);
            ConfigureMobile(importer, "iPhone", isRoom ? 2048 : isThumbnail ? 512 : 1024);
        }

        private static void ConfigureMobile(TextureImporter importer, string platform, int maxSize)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 70;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
