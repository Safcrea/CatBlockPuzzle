using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatMetaProgressionTests
    {
        private CatMetaCatalog catalog;
        private int[] rewards;

        [SetUp]
        public void SetUp()
        {
            catalog = CatMetaCatalog.Load();
            rewards = CatMetaCatalogValidator.LoadConfiguredLevelRewards();
        }

        [Test]
        public void Catalog_MapsEveryLevelToOneTenLevelChapter()
        {
            Assert.That(catalog.Chapters.Count, Is.EqualTo(10));

            for (int levelIndex = 0; levelIndex < 100; levelIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapterForLevel(levelIndex);
                Assert.That(chapter.Index, Is.EqualTo(levelIndex / 10));
                Assert.That(levelIndex, Is.InRange(chapter.FirstLevelIndex, chapter.LastLevelIndex));
                Assert.That(chapter.LevelCount, Is.EqualTo(10));
            }

            Assert.Throws<System.ArgumentOutOfRangeException>(() => catalog.GetChapterForLevel(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => catalog.GetChapterForLevel(100));
        }

        [Test]
        public void Catalog_HasExactChapterStoriesAndDecorationEconomy()
        {
            string[] expectedTitles =
            {
                "Welcome Nook",
                "Maker Corner",
                "Reading Nest",
                "Seaside Dayroom",
                "Dreamy Nursery",
                "Celebration Craft Room",
                "Tea & Trust Lounge",
                "Garden Sunroom",
                "Moonlight Den",
                "Forever Home Studio"
            };
            string[] expectedCats =
            {
                "Mochi", "Bean", "Pickle", "Nori", "Miso",
                "Waffle", "Taffy", "Sunny", "Pepper", "All Cats"
            };
            int[][] expectedCosts =
            {
                new[] { 35, 35, 35, 35, 35 },
                new[] { 40, 55, 45, 50, 55 },
                new[] { 45, 50, 60, 70, 70 },
                new[] { 70, 70, 70, 70, 70 },
                new[] { 70, 70, 80, 85, 85 },
                new[] { 85, 85, 85, 85, 85 },
                new[] { 85, 85, 90, 110, 110 },
                new[] { 105, 120, 95, 110, 110 },
                new[] { 105, 120, 110, 140, 135 },
                new[] { 130, 145, 130, 150, 165 }
            };
            int[] expectedMilestones = { 2, 4, 6, 8, 10 };

            for (int chapterIndex = 0; chapterIndex < catalog.Chapters.Count; chapterIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
                Assert.That(chapter.Title, Is.EqualTo(expectedTitles[chapterIndex]));
                Assert.That(chapter.CatName, Is.EqualTo(expectedCats[chapterIndex]));
                Assert.That(chapter.StartStory, Is.Not.Empty);
                Assert.That(chapter.CompletionStory, Is.Not.Empty);
                Assert.That(
                    chapter.BackgroundResourcePath,
                    Is.EqualTo("CatBlockPuzzle/Art/Rooms/room_" + (chapterIndex + 1).ToString("00")));
                Assert.That(chapter.Decorations.Count, Is.EqualTo(5));

                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    CatMetaDecorationDefinition decoration = chapter.Decorations[decorationIndex];
                    Assert.That(decoration.Cost, Is.EqualTo(expectedCosts[chapterIndex][decorationIndex]));
                    Assert.That(decoration.UnlockAfterChapterLevel, Is.EqualTo(expectedMilestones[decorationIndex]));
                    Assert.That(
                        decoration.SpriteResourcePath,
                        Is.EqualTo(
                            "CatBlockPuzzle/Art/Decorations/r"
                            + (chapterIndex + 1).ToString("00")
                            + "_"
                            + decoration.Id.Substring(5)));
                    Assert.That(decoration.Hotspot.x, Is.InRange(0f, 1f));
                    Assert.That(decoration.Hotspot.y, Is.InRange(0f, 1f));
                    Assert.That(decoration.Size.x, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
                    Assert.That(decoration.Size.y, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
                }
            }
        }

        [Test]
        public void Catalog_IdsAndResourcePathsAreUnique()
        {
            HashSet<string> chapterIds = new HashSet<string>();
            HashSet<string> backgroundPaths = new HashSet<string>();
            HashSet<string> decorationIds = new HashSet<string>();
            HashSet<string> decorationPaths = new HashSet<string>();

            for (int chapterIndex = 0; chapterIndex < catalog.Chapters.Count; chapterIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
                Assert.That(chapterIds.Add(chapter.Id), Is.True);
                Assert.That(backgroundPaths.Add(chapter.BackgroundResourcePath), Is.True);
                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    CatMetaDecorationDefinition decoration = chapter.Decorations[decorationIndex];
                    Assert.That(decorationIds.Add(decoration.Id), Is.True);
                    Assert.That(decorationPaths.Add(decoration.SpriteResourcePath), Is.True);
                }
            }

            Assert.That(decorationIds.Count, Is.EqualTo(50));
        }

        [Test]
        public void EveryDecoration_IsAffordableFromItsPrecedingTwoFirstClearRewards()
        {
            for (int chapterIndex = 0; chapterIndex < catalog.Chapters.Count; chapterIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
                int chapterCost = 0;
                int chapterReward = 0;
                for (int levelIndex = chapter.FirstLevelIndex; levelIndex <= chapter.LastLevelIndex; levelIndex++)
                {
                    chapterReward += rewards[levelIndex];
                }

                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    CatMetaDecorationDefinition decoration = chapter.Decorations[decorationIndex];
                    int segmentEnd = chapter.FirstLevelIndex + decoration.UnlockAfterChapterLevel - 1;
                    int twoLevelReward = rewards[segmentEnd - 1] + rewards[segmentEnd];
                    Assert.That(decoration.Cost, Is.LessThanOrEqualTo(twoLevelReward), decoration.Id);
                    chapterCost += decoration.Cost;
                }

                Assert.That(chapterCost, Is.LessThan(chapterReward), chapter.Id);
                float spendRatio = chapterCost / (float)chapterReward;
                Assert.That(spendRatio, Is.InRange(0.75f, 0.85f), chapter.Id);
            }
        }

        [Test]
        public void Validator_RejectsAnUnaffordableDecoration()
        {
            TextAsset asset = Resources.Load<TextAsset>(CatMetaCatalog.ResourcePath);
            CatMetaCatalogData data = JsonUtility.FromJson<CatMetaCatalogData>(asset.text);
            data.chapters[0].decorations[0].cost = 9999;

            bool valid = CatMetaCatalogValidator.TryValidate(data, rewards, out string error);

            Assert.That(valid, Is.False);
            StringAssert.Contains("two-level reward segment", error);
        }

        [Test]
        public void FirstClear_IsIdempotentAndUnlocksTheConfiguredMilestone()
        {
            InMemoryMetaPreferences preferences = new InMemoryMetaPreferences();
            CatMetaProgressStore store = CreateStore(preferences);
            int coins = 0;

            CatMetaOperationResult first = store.RecordFirstClear(0, rewards[0], ref coins);
            CatMetaOperationResult duplicate = store.RecordFirstClear(0, rewards[0], ref coins);
            CatMetaOperationResult second = store.RecordFirstClear(1, rewards[1], ref coins);

            Assert.That(first.Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(CatMetaOperationStatus.NoChange));
            Assert.That(second.Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(coins, Is.EqualTo(rewards[0] + rewards[1]));
            Assert.That(store.IsDecorationUnlocked("ch01_cloud_bed"), Is.True);
            Assert.That(store.IsDecorationUnlocked("ch01_bubble_ball_set"), Is.False);
        }

        [Test]
        public void PurchaseInstallAndStore_CompleteRoomAndKeepNextChapterUnlocked()
        {
            InMemoryMetaPreferences preferences = new InMemoryMetaPreferences();
            CatMetaProgressStore store = CreateStore(preferences);
            int coins = 0;
            for (int levelIndex = 0; levelIndex < 10; levelIndex++)
            {
                Assert.That(
                    store.RecordFirstClear(levelIndex, rewards[levelIndex], ref coins).Status,
                    Is.EqualTo(CatMetaOperationStatus.Success));
            }

            CatMetaChapterDefinition chapter = catalog.GetChapter(0);
            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                CatMetaDecorationDefinition decoration = chapter.Decorations[i];
                Assert.That(store.TryPurchase(decoration.Id, ref coins).Status, Is.EqualTo(CatMetaOperationStatus.Success));
                CatMetaOperationResult installed = store.TryInstall(decoration.Id);
                Assert.That(installed.Status, Is.EqualTo(CatMetaOperationStatus.Success));
                Assert.That(installed.ChapterJustCompleted, Is.EqualTo(i == chapter.Decorations.Count - 1));
            }

            Assert.That(store.IsChapterComplete(0), Is.True);
            Assert.That(store.HighestUnlockedChapter, Is.EqualTo(1));
            Assert.That(store.IsChapterUnlocked(1), Is.True);
            Assert.That(store.TryPurchase(chapter.Decorations[0].Id, ref coins).Status, Is.EqualTo(CatMetaOperationStatus.AlreadyOwned));

            Assert.That(store.Store(chapter.Decorations[0].Id).Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(store.IsInstalled(chapter.Decorations[0].Id), Is.False);
            Assert.That(store.IsOwned(chapter.Decorations[0].Id), Is.True);
            Assert.That(store.IsChapterComplete(0), Is.True, "A completed-room achievement must not be revoked by storage.");
            Assert.That(store.IsChapterUnlocked(1), Is.True);
        }

        [Test]
        public void Progress_PersistsRoundTripWithCoinsAndStoryFlags()
        {
            InMemoryMetaPreferences preferences = new InMemoryMetaPreferences();
            CatMetaProgressStore firstStore = CreateStore(preferences);
            int coins = 0;
            firstStore.RecordFirstClear(0, rewards[0], ref coins);
            firstStore.RecordFirstClear(1, rewards[1], ref coins);
            Assert.That(firstStore.TryPurchase("ch01_cloud_bed", ref coins).Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(firstStore.TryInstall("ch01_cloud_bed").Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(firstStore.MarkStoryViewed(CatMetaStoryIds.Intro("welcome_nook")).Changed, Is.True);

            CatMetaProgressStore reloaded = CreateStore(preferences);

            Assert.That(reloaded.IsLevelFirstCleared(0), Is.True);
            Assert.That(reloaded.IsLevelFirstCleared(1), Is.True);
            Assert.That(reloaded.IsOwned("ch01_cloud_bed"), Is.True);
            Assert.That(reloaded.IsInstalled("ch01_cloud_bed"), Is.True);
            Assert.That(reloaded.HasViewedStory(CatMetaStoryIds.Intro("welcome_nook")), Is.True);
            Assert.That(reloaded.CoinBalance, Is.EqualTo(10));
            Assert.That(preferences.SaveCount, Is.GreaterThan(0));
        }

        [Test]
        public void LegacyMigration_FurnishesOnlyStrictlyPriorChaptersAndPreservesCoins()
        {
            InMemoryMetaPreferences preferences = new InMemoryMetaPreferences();
            CatMetaStorageKeys keys = TestKeys;
            preferences.SetInt(keys.CoinsKey, 777);
            CatMetaProgressStore store = CatMetaProgressStore.Load(catalog, preferences, keys);

            CatMetaOperationResult migrated = store.MigrateLegacyProgress(25);

            Assert.That(migrated.Status, Is.EqualTo(CatMetaOperationStatus.Success));
            Assert.That(store.CoinBalance, Is.EqualTo(777));
            Assert.That(store.IsChapterComplete(0), Is.True);
            Assert.That(store.IsChapterComplete(1), Is.True);
            Assert.That(store.IsChapterComplete(2), Is.False);
            Assert.That(store.OwnedDecorationCount(0), Is.EqualTo(5));
            Assert.That(store.InstalledDecorationCount(1), Is.EqualTo(5));
            Assert.That(store.OwnedDecorationCount(2), Is.EqualTo(0));
            Assert.That(store.IsLevelFirstCleared(24), Is.True);
            Assert.That(store.IsLevelFirstCleared(25), Is.False);
            Assert.That(store.HighestUnlockedChapter, Is.EqualTo(2));
            Assert.That(store.HasViewedStory(CatMetaStoryIds.Completion("maker_corner")), Is.True);
            Assert.That(store.MigrateLegacyProgress(95).Status, Is.EqualTo(CatMetaOperationStatus.NoChange));
            Assert.That(store.IsChapterComplete(2), Is.False, "Migration must be one-shot.");
        }

        [Test]
        public void PurchaseValidation_RejectsLockedUnknownAndUnaffordableItems()
        {
            InMemoryMetaPreferences preferences = new InMemoryMetaPreferences();
            CatMetaProgressStore store = CreateStore(preferences);
            int coins = 1000;

            Assert.That(store.TryPurchase("missing", ref coins).Status, Is.EqualTo(CatMetaOperationStatus.UnknownDecoration));
            Assert.That(store.TryPurchase("ch01_cloud_bed", ref coins).Status, Is.EqualTo(CatMetaOperationStatus.DecorationLocked));
            Assert.That(store.TryPurchase("ch02_patchwork_cushion", ref coins).Status, Is.EqualTo(CatMetaOperationStatus.ChapterLocked));

            store.RecordFirstClear(0, rewards[0], ref coins);
            store.RecordFirstClear(1, rewards[1], ref coins);
            coins = 0;
            Assert.That(store.TryPurchase("ch01_cloud_bed", ref coins).Status, Is.EqualTo(CatMetaOperationStatus.InsufficientCoins));
            Assert.That(store.TryInstall("ch01_cloud_bed").Status, Is.EqualTo(CatMetaOperationStatus.NotOwned));
        }

        private CatMetaProgressStore CreateStore(InMemoryMetaPreferences preferences)
        {
            return CatMetaProgressStore.Load(catalog, preferences, TestKeys);
        }

        private static CatMetaStorageKeys TestKeys => new CatMetaStorageKeys(
            "tests.meta.progress",
            "tests.meta.coins",
            "tests.meta.level");

        private sealed class InMemoryMetaPreferences : ICatMetaPreferences
        {
            private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
            private readonly Dictionary<string, int> integers = new Dictionary<string, int>();

            public int SaveCount { get; private set; }

            public bool HasKey(string key)
            {
                return strings.ContainsKey(key) || integers.ContainsKey(key);
            }

            public string GetString(string key, string defaultValue)
            {
                return strings.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public int GetInt(string key, int defaultValue)
            {
                return integers.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                strings[key] = value;
            }

            public void SetInt(string key, int value)
            {
                integers[key] = value;
            }

            public void Save()
            {
                SaveCount++;
            }
        }
    }
}
