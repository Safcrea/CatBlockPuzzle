using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal enum CatMetaOperationStatus
    {
        Success = 0,
        NoChange = 1,
        InvalidLevel = 2,
        InvalidReward = 3,
        UnknownChapter = 4,
        UnknownDecoration = 5,
        ChapterLocked = 6,
        DecorationLocked = 7,
        AlreadyOwned = 8,
        InsufficientCoins = 9,
        NotOwned = 10,
        AlreadyInstalled = 11,
        NotInstalled = 12,
        InvalidStoryId = 13
    }

    internal readonly struct CatMetaOperationResult
    {
        public CatMetaOperationStatus Status { get; }
        public int CoinBalance { get; }
        public int CoinDelta { get; }
        public bool ChapterJustCompleted { get; }

        public bool Succeeded => Status == CatMetaOperationStatus.Success
            || Status == CatMetaOperationStatus.NoChange;
        public bool Changed => Status == CatMetaOperationStatus.Success;

        internal CatMetaOperationResult(
            CatMetaOperationStatus status,
            int coinBalance,
            int coinDelta = 0,
            bool chapterJustCompleted = false)
        {
            Status = status;
            CoinBalance = coinBalance;
            CoinDelta = coinDelta;
            ChapterJustCompleted = chapterJustCompleted;
        }
    }

    [Serializable]
    internal sealed class CatMetaSaveData
    {
        public int version = CatMetaProgressStore.CurrentSaveVersion;
        public int legacyMigrationVersion;
        public List<string> ownedDecorationIds = new List<string>();
        public List<string> installedDecorationIds = new List<string>();
        public List<string> completedChapterIds = new List<string>();
        public List<string> viewedStoryIds = new List<string>();
        public List<int> firstClearedLevelIndices = new List<int>();
        public List<int> skippedLevelIndices = new List<int>();
    }

    internal readonly struct CatMetaStorageKeys
    {
        public readonly string SaveDataKey;
        public readonly string CoinsKey;
        public readonly string LegacyLevelKey;

        public CatMetaStorageKeys(string saveDataKey, string coinsKey, string legacyLevelKey)
        {
            SaveDataKey = saveDataKey;
            CoinsKey = coinsKey;
            LegacyLevelKey = legacyLevelKey;
        }

        public static CatMetaStorageKeys Production => new CatMetaStorageKeys(
            "CatBlockPuzzle.Meta.Progress",
            "CatBlockPuzzle.Coins",
            "CatBlockPuzzle.LevelIndex");
    }

    internal interface ICatMetaPreferences
    {
        bool HasKey(string key);
        string GetString(string key, string defaultValue);
        int GetInt(string key, int defaultValue);
        void SetString(string key, string value);
        void SetInt(string key, int value);
        void Save();
    }

    internal sealed class UnityCatMetaPreferences : ICatMetaPreferences
    {
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }

    internal sealed class CatMetaProgressStore
    {
        public const int CurrentSaveVersion = 1;
        public const int CurrentLegacyMigrationVersion = 1;

        private readonly CatMetaCatalog catalog;
        private readonly ICatMetaPreferences preferences;
        private readonly CatMetaStorageKeys keys;
        private readonly HashSet<string> ownedDecorationIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> installedDecorationIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> completedChapterIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> viewedStoryIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> firstClearedLevelIndices = new HashSet<int>();
        private readonly HashSet<int> skippedLevelIndices = new HashSet<int>();
        private CatMetaSaveData data;

        public int CoinBalance => preferences.GetInt(keys.CoinsKey, 0);

        public int HighestUnlockedChapter
        {
            get
            {
                int highest = 0;
                for (int chapterIndex = 0; chapterIndex < CatMetaCatalog.ChapterCount - 1; chapterIndex++)
                {
                    // Room decoration is optional. A chapter unlocks the next one when
                    // its gameplay levels have been cleared or explicitly skipped.
                    if (!AreAllChapterLevelsCleared(chapterIndex))
                    {
                        break;
                    }

                    highest = chapterIndex + 1;
                }

                return highest;
            }
        }

        private CatMetaProgressStore(
            CatMetaCatalog catalog,
            ICatMetaPreferences preferences,
            CatMetaStorageKeys keys)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            this.keys = keys;
            ValidateStorageKeys(keys);
            LoadData();
        }

        public static CatMetaProgressStore Load(
            CatMetaCatalog catalog,
            int savedLevelIndex,
            int coins)
        {
            CatMetaProgressStore store = new CatMetaProgressStore(
                catalog,
                new UnityCatMetaPreferences(),
                CatMetaStorageKeys.Production);

            if (!store.preferences.HasKey(store.keys.CoinsKey))
            {
                store.preferences.SetInt(store.keys.CoinsKey, Mathf.Max(0, coins));
            }

            store.MigrateLegacyProgress(savedLevelIndex);
            return store;
        }

        internal static CatMetaProgressStore Load(
            CatMetaCatalog catalog,
            ICatMetaPreferences preferences,
            CatMetaStorageKeys keys)
        {
            return new CatMetaProgressStore(catalog, preferences, keys);
        }

        public bool IsOwned(string decorationId)
        {
            return !string.IsNullOrEmpty(decorationId) && ownedDecorationIds.Contains(decorationId);
        }

        public bool IsInstalled(string decorationId)
        {
            return !string.IsNullOrEmpty(decorationId) && installedDecorationIds.Contains(decorationId);
        }

        public bool IsLevelFirstCleared(int zeroBasedLevelIndex)
        {
            return firstClearedLevelIndices.Contains(zeroBasedLevelIndex);
        }

        public bool IsLevelPassed(int zeroBasedLevelIndex) => firstClearedLevelIndices.Contains(zeroBasedLevelIndex)
            || skippedLevelIndices.Contains(zeroBasedLevelIndex);

        public CatMetaOperationResult RecordSkip(int zeroBasedLevelIndex)
        {
            if (zeroBasedLevelIndex < 0 || zeroBasedLevelIndex >= CatMetaCatalog.SupportedLevelCount)
                return Result(CatMetaOperationStatus.InvalidLevel, CoinBalance);
            if (!IsChapterUnlocked(catalog.GetChapterForLevel(zeroBasedLevelIndex).Index))
                return Result(CatMetaOperationStatus.ChapterLocked, CoinBalance);
            if (IsLevelPassed(zeroBasedLevelIndex)) return Result(CatMetaOperationStatus.NoChange, CoinBalance);
            skippedLevelIndices.Add(zeroBasedLevelIndex);
            Save();
            return Result(CatMetaOperationStatus.Success, CoinBalance);
        }

        public bool HasViewedStory(string storyId)
        {
            return !string.IsNullOrEmpty(storyId) && viewedStoryIds.Contains(storyId);
        }

        public bool IsChapterComplete(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= CatMetaCatalog.ChapterCount)
            {
                return false;
            }

            return completedChapterIds.Contains(catalog.GetChapter(chapterIndex).Id);
        }

        public bool IsChapterUnlocked(int chapterIndex)
        {
            return chapterIndex >= 0
                && chapterIndex < CatMetaCatalog.ChapterCount
                && chapterIndex <= HighestUnlockedChapter;
        }

        public bool IsDecorationUnlocked(string decorationId)
        {
            if (!catalog.TryGetDecoration(decorationId, out CatMetaDecorationDefinition decoration)
                || !IsChapterUnlocked(decoration.ChapterIndex))
            {
                return false;
            }

            CatMetaChapterDefinition chapter = catalog.GetChapter(decoration.ChapterIndex);
            int requiredLevelIndex = chapter.FirstLevelIndex + decoration.UnlockAfterChapterLevel - 1;
            return firstClearedLevelIndices.Contains(requiredLevelIndex);
        }

        public int OwnedDecorationCount(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= CatMetaCatalog.ChapterCount)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<CatMetaDecorationDefinition> decorations = catalog.GetChapter(chapterIndex).Decorations;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (ownedDecorationIds.Contains(decorations[i].Id))
                {
                    count++;
                }
            }

            return count;
        }

        public int InstalledDecorationCount(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= CatMetaCatalog.ChapterCount)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<CatMetaDecorationDefinition> decorations = catalog.GetChapter(chapterIndex).Decorations;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (installedDecorationIds.Contains(decorations[i].Id))
                {
                    count++;
                }
            }

            return count;
        }

        public CatMetaOperationResult RecordFirstClear(
            int zeroBasedLevelIndex,
            int reward,
            ref int coins)
        {
            if (zeroBasedLevelIndex < 0 || zeroBasedLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return Result(CatMetaOperationStatus.InvalidLevel, coins);
            }

            if (reward < 0)
            {
                return Result(CatMetaOperationStatus.InvalidReward, coins);
            }

            CatMetaChapterDefinition chapter = catalog.GetChapterForLevel(zeroBasedLevelIndex);
            if (!IsChapterUnlocked(chapter.Index))
            {
                return Result(CatMetaOperationStatus.ChapterLocked, coins);
            }

            if (!firstClearedLevelIndices.Add(zeroBasedLevelIndex))
            {
                return Result(CatMetaOperationStatus.NoChange, coins);
            }

            int safeCoins = Mathf.Max(0, coins);
            long increased = (long)safeCoins + reward;
            coins = increased > int.MaxValue ? int.MaxValue : (int)increased;
            preferences.SetInt(keys.CoinsKey, coins);
            Save();
            return new CatMetaOperationResult(
                CatMetaOperationStatus.Success,
                coins,
                coins - safeCoins);
        }

        public CatMetaOperationResult TryPurchase(string decorationId, ref int coins)
        {
            if (!catalog.TryGetDecoration(decorationId, out CatMetaDecorationDefinition decoration))
            {
                return Result(CatMetaOperationStatus.UnknownDecoration, coins);
            }

            if (ownedDecorationIds.Contains(decorationId))
            {
                return Result(CatMetaOperationStatus.AlreadyOwned, coins);
            }

            if (!IsChapterUnlocked(decoration.ChapterIndex))
            {
                return Result(CatMetaOperationStatus.ChapterLocked, coins);
            }

            if (!IsDecorationUnlocked(decorationId))
            {
                return Result(CatMetaOperationStatus.DecorationLocked, coins);
            }

            int safeCoins = Mathf.Max(0, coins);
            if (safeCoins < decoration.Cost)
            {
                return Result(CatMetaOperationStatus.InsufficientCoins, coins);
            }

            coins = safeCoins - decoration.Cost;
            ownedDecorationIds.Add(decorationId);
            preferences.SetInt(keys.CoinsKey, coins);
            Save();
            return new CatMetaOperationResult(
                CatMetaOperationStatus.Success,
                coins,
                -decoration.Cost);
        }

        public CatMetaOperationResult TryPurchase(string decorationId)
        {
            int coins = CoinBalance;
            return TryPurchase(decorationId, ref coins);
        }

        public CatMetaOperationResult TryInstall(string decorationId)
        {
            if (!catalog.TryGetDecoration(decorationId, out CatMetaDecorationDefinition decoration))
            {
                return Result(CatMetaOperationStatus.UnknownDecoration, CoinBalance);
            }

            if (!ownedDecorationIds.Contains(decorationId))
            {
                return Result(CatMetaOperationStatus.NotOwned, CoinBalance);
            }

            if (!installedDecorationIds.Add(decorationId))
            {
                return Result(CatMetaOperationStatus.AlreadyInstalled, CoinBalance);
            }

            bool chapterJustCompleted = AwardCompletionIfFullyInstalled(decoration.ChapterIndex);
            Save();
            return new CatMetaOperationResult(
                CatMetaOperationStatus.Success,
                CoinBalance,
                0,
                chapterJustCompleted);
        }

        public CatMetaOperationResult Store(string decorationId)
        {
            if (!catalog.TryGetDecoration(decorationId, out CatMetaDecorationDefinition decoration))
            {
                return Result(CatMetaOperationStatus.UnknownDecoration, CoinBalance);
            }

            if (!ownedDecorationIds.Contains(decorationId))
            {
                return Result(CatMetaOperationStatus.NotOwned, CoinBalance);
            }

            if (!installedDecorationIds.Remove(decorationId))
            {
                return Result(CatMetaOperationStatus.NotInstalled, CoinBalance);
            }

            Save();
            return Result(CatMetaOperationStatus.Success, CoinBalance);
        }

        public CatMetaOperationResult MarkStoryViewed(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                return Result(CatMetaOperationStatus.InvalidStoryId, CoinBalance);
            }

            if (!viewedStoryIds.Add(storyId))
            {
                return Result(CatMetaOperationStatus.NoChange, CoinBalance);
            }

            Save();
            return Result(CatMetaOperationStatus.Success, CoinBalance);
        }

        public CatMetaOperationResult MigrateLegacyProgress(int legacySavedLevelIndex)
        {
            if (data.legacyMigrationVersion >= CurrentLegacyMigrationVersion)
            {
                return Result(CatMetaOperationStatus.NoChange, CoinBalance);
            }

            int safeSavedLevelIndex = Mathf.Clamp(
                legacySavedLevelIndex,
                0,
                CatMetaCatalog.SupportedLevelCount - 1);
            int savedChapterIndex = safeSavedLevelIndex / CatMetaCatalog.LevelsPerChapter;

            // A saved level is the legacy current-level index. Mark earlier levels as
            // first-cleared without paying rewards again, then furnish only chapters
            // strictly before the saved chapter so the update never re-locks progress.
            for (int levelIndex = 0; levelIndex < safeSavedLevelIndex; levelIndex++)
            {
                firstClearedLevelIndices.Add(levelIndex);
            }

            for (int chapterIndex = 0; chapterIndex < savedChapterIndex; chapterIndex++)
            {
                CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    string decorationId = chapter.Decorations[decorationIndex].Id;
                    ownedDecorationIds.Add(decorationId);
                    installedDecorationIds.Add(decorationId);
                }

                completedChapterIds.Add(chapter.Id);
                viewedStoryIds.Add(CatMetaStoryIds.Intro(chapter.Id));
                viewedStoryIds.Add(CatMetaStoryIds.TrustMilestone(chapter.Id));
                viewedStoryIds.Add(CatMetaStoryIds.Completion(chapter.Id));
            }

            data.legacyMigrationVersion = CurrentLegacyMigrationVersion;
            Save();
            return Result(CatMetaOperationStatus.Success, CoinBalance);
        }

        public void Save()
        {
            CopySetsToData();
            preferences.SetString(keys.SaveDataKey, JsonUtility.ToJson(data));
            preferences.Save();
        }

        private void LoadData()
        {
            string json = preferences.GetString(keys.SaveDataKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                data = new CatMetaSaveData();
            }
            else
            {
                try
                {
                    data = JsonUtility.FromJson<CatMetaSaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Cat meta progress could not be parsed and was reset: " + exception.Message);
                    data = new CatMetaSaveData();
                }

                if (data == null)
                {
                    data = new CatMetaSaveData();
                }
            }

            if (data.version > CurrentSaveVersion)
            {
                throw new InvalidOperationException(
                    "Cat meta save version " + data.version + " is newer than supported version "
                    + CurrentSaveVersion + ".");
            }

            data.version = CurrentSaveVersion;
            NormalizeDataLists();
            CopyDataToSets();

            for (int chapterIndex = 0; chapterIndex < CatMetaCatalog.ChapterCount; chapterIndex++)
            {
                AwardCompletionIfFullyInstalled(chapterIndex);
            }
        }

        private void NormalizeDataLists()
        {
            data.ownedDecorationIds = data.ownedDecorationIds ?? new List<string>();
            data.installedDecorationIds = data.installedDecorationIds ?? new List<string>();
            data.completedChapterIds = data.completedChapterIds ?? new List<string>();
            data.viewedStoryIds = data.viewedStoryIds ?? new List<string>();
            data.firstClearedLevelIndices = data.firstClearedLevelIndices ?? new List<int>();
            data.skippedLevelIndices = data.skippedLevelIndices ?? new List<int>();
        }

        private void CopyDataToSets()
        {
            ownedDecorationIds.Clear();
            installedDecorationIds.Clear();
            completedChapterIds.Clear();
            viewedStoryIds.Clear();
            firstClearedLevelIndices.Clear();
            skippedLevelIndices.Clear();

            for (int i = 0; i < data.ownedDecorationIds.Count; i++)
            {
                string decorationId = data.ownedDecorationIds[i];
                if (catalog.TryGetDecoration(decorationId, out _))
                {
                    ownedDecorationIds.Add(decorationId);
                }
            }

            for (int i = 0; i < data.installedDecorationIds.Count; i++)
            {
                string decorationId = data.installedDecorationIds[i];
                if (ownedDecorationIds.Contains(decorationId))
                {
                    installedDecorationIds.Add(decorationId);
                }
            }

            for (int i = 0; i < data.completedChapterIds.Count; i++)
            {
                string chapterId = data.completedChapterIds[i];
                if (catalog.TryGetChapter(chapterId, out _))
                {
                    completedChapterIds.Add(chapterId);
                }
            }

            for (int i = 0; i < data.viewedStoryIds.Count; i++)
            {
                string storyId = data.viewedStoryIds[i];
                if (!string.IsNullOrWhiteSpace(storyId))
                {
                    viewedStoryIds.Add(storyId);
                }
            }

            for (int i = 0; i < data.firstClearedLevelIndices.Count; i++)
            {
                int levelIndex = data.firstClearedLevelIndices[i];
                if (levelIndex >= 0 && levelIndex < CatMetaCatalog.SupportedLevelCount)
                {
                    firstClearedLevelIndices.Add(levelIndex);
                }
            }
            for (int i = 0; i < data.skippedLevelIndices.Count; i++)
            {
                int levelIndex = data.skippedLevelIndices[i];
                if (levelIndex >= 0 && levelIndex < CatMetaCatalog.SupportedLevelCount)
                    skippedLevelIndices.Add(levelIndex);
            }
        }

        private void CopySetsToData()
        {
            data.version = CurrentSaveVersion;
            data.ownedDecorationIds = SortedStrings(ownedDecorationIds);
            data.installedDecorationIds = SortedStrings(installedDecorationIds);
            data.completedChapterIds = SortedStrings(completedChapterIds);
            data.viewedStoryIds = SortedStrings(viewedStoryIds);
            data.firstClearedLevelIndices = new List<int>(firstClearedLevelIndices);
            data.firstClearedLevelIndices.Sort();
            data.skippedLevelIndices = new List<int>(skippedLevelIndices);
            data.skippedLevelIndices.Sort();
        }

        private bool AwardCompletionIfFullyInstalled(int chapterIndex)
        {
            CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                if (!installedDecorationIds.Contains(chapter.Decorations[i].Id))
                {
                    return false;
                }
            }

            return completedChapterIds.Add(chapter.Id);
        }

        private bool AreAllChapterLevelsCleared(int chapterIndex)
        {
            CatMetaChapterDefinition chapter = catalog.GetChapter(chapterIndex);
            for (int levelIndex = chapter.FirstLevelIndex; levelIndex <= chapter.LastLevelIndex; levelIndex++)
            {
                if (!IsLevelPassed(levelIndex))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> SortedStrings(HashSet<string> values)
        {
            List<string> sorted = new List<string>(values);
            sorted.Sort(StringComparer.Ordinal);
            return sorted;
        }

        private static CatMetaOperationResult Result(CatMetaOperationStatus status, int coins)
        {
            return new CatMetaOperationResult(status, Mathf.Max(0, coins));
        }

        private static void ValidateStorageKeys(CatMetaStorageKeys storageKeys)
        {
            if (string.IsNullOrWhiteSpace(storageKeys.SaveDataKey)
                || string.IsNullOrWhiteSpace(storageKeys.CoinsKey)
                || string.IsNullOrWhiteSpace(storageKeys.LegacyLevelKey))
            {
                throw new ArgumentException("Meta storage keys cannot be empty.", nameof(storageKeys));
            }
        }
    }
}
