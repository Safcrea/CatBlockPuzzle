using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatMetaAssetTests
    {
        [Test]
        public void CatalogResources_AllRoomAndDecorationTexturesExist()
        {
            CatMetaCatalog catalog = CatMetaCatalog.Load();
            for (int chapterIndex = 0; chapterIndex < CatMetaCatalog.ChapterCount; chapterIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
                Texture2D room = Resources.Load<Texture2D>(chapter.BackgroundResourcePath);
                Assert.That(room, Is.Not.Null, "Missing room texture for " + chapter.Id);
                Assert.That(room.height, Is.GreaterThan(room.width), chapter.Id + " room must remain portrait.");

                Texture2D thumbnail = Resources.Load<Texture2D>(chapter.ThumbnailResourcePath);
                Assert.That(thumbnail, Is.Not.Null, "Missing room thumbnail for " + chapter.Id);
                Assert.That(thumbnail.width, Is.LessThanOrEqualTo(512));
                Assert.That(thumbnail.height, Is.LessThanOrEqualTo(512));

                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    CatMetaDecorationDefinition decoration = chapter.Decorations[decorationIndex];
                    Texture2D texture = Resources.Load<Texture2D>(decoration.SpriteResourcePath);
                    Assert.That(texture, Is.Not.Null, "Missing decoration texture " + decoration.Id);

                    string path = AssetDatabase.GetAssetPath(texture);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, "Missing TextureImporter for " + path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                    Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput), path);
                    Assert.That(importer.alphaIsTransparency, Is.True, path);
                    Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True, path + " must contain generated alpha.");
                }
            }
        }
    }
}
