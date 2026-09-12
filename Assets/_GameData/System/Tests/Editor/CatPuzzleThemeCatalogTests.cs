using NUnit.Framework;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzleThemeCatalogTests
    {
        [Test]
        public void FirstTwentyFiveLevels_ShowcaseEveryThemeInOrder()
        {
            for (int theme = 0; theme < CatPuzzleThemeCatalog.ThemeCount; theme++)
            {
                for (int offset = 0; offset < CatPuzzleThemeCatalog.LevelsPerTheme; offset++)
                {
                    int level = (theme * CatPuzzleThemeCatalog.LevelsPerTheme) + offset;
                    Assert.That(CatPuzzleThemeCatalog.GetThemeIndexForLevel(level), Is.EqualTo(theme));
                }
            }
        }

        [Test]
        public void HundredLevelSequence_IsSeededBalancedAndChangesEveryFiveLevels()
        {
            int[] expectedBlocks =
            {
                0, 1, 2, 3, 4,
                3, 1, 2, 0, 4,
                3, 1, 0, 2, 4,
                0, 4, 3, 1, 2
            };
            int[] actualBlocks = CatPuzzleThemeCatalog.GetThemeBlockSequence();
            CollectionAssert.AreEqual(expectedBlocks, actualBlocks);

            int[] blockCounts = new int[CatPuzzleThemeCatalog.ThemeCount];
            for (int block = 0; block < actualBlocks.Length; block++)
            {
                int theme = actualBlocks[block];
                blockCounts[theme]++;
                if (block > 0)
                {
                    Assert.That(theme, Is.Not.EqualTo(actualBlocks[block - 1]), "Adjacent theme blocks repeated.");
                }

                int firstLevel = block * CatPuzzleThemeCatalog.LevelsPerTheme;
                for (int offset = 0; offset < CatPuzzleThemeCatalog.LevelsPerTheme; offset++)
                {
                    Assert.That(CatPuzzleThemeCatalog.GetThemeIndexForLevel(firstLevel + offset), Is.EqualTo(theme));
                }
            }

            for (int theme = 0; theme < blockCounts.Length; theme++)
            {
                Assert.That(blockCounts[theme], Is.EqualTo(4), "Theme did not receive an equal share of the 100 levels.");
            }
        }

        [Test]
        public void Themes_HaveUniqueAtlasPanelsAndNames()
        {
            for (int theme = 0; theme < CatPuzzleThemeCatalog.ThemeCount; theme++)
            {
                CatPuzzleTheme definition = CatPuzzleThemeCatalog.GetTheme(theme);
                Assert.That(definition.Index, Is.EqualTo(theme));
                Assert.That(definition.AtlasIndex, Is.EqualTo(theme));
                Assert.That(definition.Id, Is.Not.Empty);
                Assert.That(definition.DisplayName, Is.Not.Empty);
            }
        }
    }
}
