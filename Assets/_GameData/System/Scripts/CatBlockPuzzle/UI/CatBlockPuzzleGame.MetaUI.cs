using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Runtime-only presentation for the room rescue meta game.  Keeping this in a
    /// partial lets the existing scene-free game continue to bootstrap itself.
    /// </summary>
    public sealed partial class CatBlockPuzzleGame
    {
        private const string MetaTelemetryPrefix = "[CatMetaTelemetry] ";

        private readonly Dictionary<string, Sprite> metaResourceSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> missingMetaResources =
            new HashSet<string>(StringComparer.Ordinal);

        private CatMetaCatalog metaCatalog;
        private CatMetaProgressStore metaProgress;
        private bool metaSystemsUnavailable;
        private bool metaOverlayOwnsPause;
        private bool metaTimerWasRunning;
        private bool metaRequiresRoomCompletion;
        private int metaCurrentChapterIndex;
        private int metaLastAwardedCoins;
        private string metaSelectedDecorationId;

        private bool IsMetaUiOpen
        {
            get
            {
                return metaOverlay != null
                    && metaOverlay.gameObject.activeInHierarchy
                    && ((metaHubPage != null && metaHubPage.gameObject.activeSelf)
                        || (metaRoomPage != null && metaRoomPage.gameObject.activeSelf)
                        || (metaStoryOverlay != null && metaStoryOverlay.gameObject.activeSelf)
                        || (metaCompletionOverlay != null && metaCompletionOverlay.gameObject.activeSelf));
            }
        }

        /// <summary>Loads and migrates the meta catalog/save without creating UI.</summary>
        private void InitializeMetaSystems()
        {
            if (metaCatalog != null || metaSystemsUnavailable)
            {
                return;
            }

            try
            {
                int savedLevel = PlayerPrefs.GetInt(SavedLevelKey, 0);
                int savedCoins = PlayerPrefs.GetInt(SavedCoinsKey, Mathf.Max(0, coins));
                metaCatalog = contentCatalog.CreateMetaCatalog();
                metaProgress = CatMetaProgressStore.Load(metaCatalog, savedLevel, savedCoins);
                coins = metaProgress.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                LogMetaEvent("systems_initialized",
                    "catalogVersion=" + CatMetaCatalog.CurrentVersion
                    + " highestChapter=" + metaProgress.HighestUnlockedChapter);
            }
            catch (Exception exception)
            {
                metaSystemsUnavailable = true;
                metaCatalog = null;
                metaProgress = null;
                Debug.LogError("Cat meta systems were disabled: " + exception);
            }
        }

        /// <summary>Builds the persistent overlay containers and rewires Home/Next.</summary>


        /// <summary>Shows the ten-room map and its dominant continuation action.</summary>
        private void OpenRoomHub()
        {
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = Mathf.Clamp(
                metaProgress.HighestUnlockedChapter,
                0,
                metaCatalog.Chapters.Count - 1);
            metaSelectedDecorationId = null;
            metaRoomPage.gameObject.SetActive(false);
            metaStoryOverlay.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaHubPage.gameObject.SetActive(true);
            RenderRoomHub();
            LogMetaEvent("hub_open", "recommendedChapter=" + metaCurrentChapterIndex);
        }

        /// <summary>Shows one furnished room, its paw track, and five item cards.</summary>
        private void OpenRoomDetail(int chapterIndex)
        {
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            if (chapterIndex < 0
                || chapterIndex >= metaCatalog.Chapters.Count
                || !metaProgress.IsChapterUnlocked(chapterIndex))
            {
                ShowMetaStatus("Finish the previous room to unlock this chapter.");
                LogMetaEvent("room_open_blocked", "chapter=" + chapterIndex);
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = chapterIndex;
            metaHubPage.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(true);
            SelectFirstStoredDecoration(chapterIndex);
            RenderRoomDetail(chapterIndex);

            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            LogMetaEvent("room_open",
                "chapter=" + chapterIndex
                + " installed=" + metaProgress.InstalledDecorationCount(chapterIndex));

            string introId = CatMetaStoryIds.Intro(chapter.Id);
            if (!metaProgress.HasViewedStory(introId))
            {
                ShowChapterIntro(chapterIndex, null);
            }
        }

        /// <summary>Dismisses meta UI and safely resumes the interrupted puzzle.</summary>
        private void CloseMetaToGameplay()
        {
            if (GameSystem.Instance != null && GameSystem.Instance.HasReferenceUi) { GameSystem.Instance.GoHome(); return; }
            if (metaRequiresRoomCompletion
                && metaProgress != null
                && metaCatalog != null
                && !metaProgress.IsChapterComplete(metaCurrentChapterIndex))
            {
                OpenRoomDetail(metaCurrentChapterIndex);
                ShowMetaStatus("Place all five decorations to open the next room.");
                return;
            }

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            bool resumeTimer = metaOverlayOwnsPause && metaTimerWasRunning;
            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;

            if (levelFailed || IsResultScreenOpen || (GameSystem.Instance != null && GameSystem.Instance.IsSettingsOpen))
            {
                return;
            }

            inputLocked = false;
            SetTrayScrollEnabled(true);
            if (resumeTimer
                || (!timerRunning && boardRevealRoutine == null && activeLevel != null))
            {
                StartLevelTimer();
            }
        }

        /// <summary>Applies the active chapter's full-screen authored background.</summary>
        private void ApplyMetaLevelPresentation(int zeroBasedLevelIndex)
        {
            if (metaCatalog == null)
            {
                InitializeMetaSystems();
            }

            if (metaCatalog == null
                || zeroBasedLevelIndex < 0
                || zeroBasedLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(zeroBasedLevelIndex);
            Sprite roomSprite = contentCatalog.roomBackgrounds[chapter.Index];
            if (backgroundImage != null && roomSprite != null && backgroundImage.sprite != roomSprite)
            {
                CrossfadeGameplayBackground(roomSprite);
            }

            if (objectiveText != null && activeLevel != null)
            {
                objectiveText.text = chapter.Title + "  •  " + activeLevel.Title;
            }
        }

        /// <summary>
        /// Loads the requested puzzle behind either the first-run rescue cards or the
        /// returning-player hub. Editor previews intentionally bypass meta UI.
        /// </summary>
        private void ShowMetaStartup(int requestedLevelIndex, bool isEditorPreview)
        {
            InitializeMetaSystems();
            int levelCount = levelManager != null ? levelManager.LevelCount : 0;
            if (levelCount <= 0)
            {
                return;
            }

            int safeLevelIndex = Mathf.Clamp(requestedLevelIndex, 0, levelCount - 1);
            LoadLevel(safeLevelIndex, !isEditorPreview);
            ApplyMetaLevelPresentation(safeLevelIndex);

            if (isEditorPreview || metaProgress == null || metaCatalog == null)
            {
                CloseMetaToGameplay();
                return;
            }

            CatMetaChapterDefinition firstChapter = metaCatalog.GetChapter(0);
            string firstIntroId = CatMetaStoryIds.Intro(firstChapter.Id);
            if (!metaProgress.HasViewedStory(firstIntroId))
            {
                ShowFirstRunStory();
            }
            else
            {
                OpenRoomHub();
            }
        }

        /// <summary>Records an idempotent first clear and returns the actual coin delta.</summary>
        private int RecordMetaFirstClear(int reward)
        {
            if (levelNavigationTesting)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            InitializeMetaSystems();
            if (metaProgress == null)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            CatMetaOperationResult result = metaProgress.RecordFirstClear(levelIndex, reward, ref coins);
            metaLastAwardedCoins = Mathf.Max(0, result.CoinDelta);
            if (coinText != null)
            {
                coinText.text = coins.ToString();
            }

            LogMetaEvent("level_first_clear",
                "level=" + levelIndex
                + " rewardRequested=" + reward
                + " coinDelta=" + result.CoinDelta
                + " status=" + result.Status);
            return metaLastAwardedCoins;
        }

        /// <summary>Overlays decoration/trust milestones on the existing win panel.</summary>
        private void ConfigureMetaWinPresentation(int awardedCoins)
        {
            metaLastAwardedCoins = Mathf.Max(0, awardedCoins);
            if (metaCatalog == null || metaProgress == null)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            // Decoration is not part of the current result flow. Keep its button hidden
            // and preserve the authored, full-width Continue artwork below.
            if (metaDecorateNowButton != null)
            {
                metaDecorateNowButton.gameObject.SetActive(false);
            }

            if (metaNextLevelButtonText != null)
            {
                metaNextLevelButtonText.text = chapterLevel == CatMetaCatalog.LevelsPerChapter
                    && !metaProgress.IsChapterComplete(chapter.Index)
                    ? "Decorate Room"
                    : "Next Level";
            }
        }

        /// <summary>Advances directly through gameplay; room furnishing is optional UI.</summary>
        private void LoadNextLevelThroughMetaGate()
        {
            if (metaCatalog == null || metaProgress == null)
            {
                LoadNextLevel();
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            levelCompleteScreen?.Hide();

            ContinueAfterMetaWin(chapter, chapterLevel);
        }

        private void ContinueAfterMetaWin(CatMetaChapterDefinition chapter, int chapterLevel)
        {
            if (chapterLevel >= CatMetaCatalog.LevelsPerChapter)
            {
                if (chapter.Index >= metaCatalog.Chapters.Count - 1)
                {
                    GameSystem.Instance?.GoHome();
                    return;
                }

                PlayMetaLevel(metaCatalog.GetChapter(chapter.Index + 1).FirstLevelIndex);
                return;
            }

            PlayMetaLevel(levelIndex + 1);
        }















        private void OnDecorationCardPressed(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            if (!metaProgress.IsDecorationUnlocked(decorationId))
            {
                ShowMetaStatus("Clear level " + decoration.UnlockAfterChapterLevel + " in this room first.");
                LogMetaEvent("decoration_locked", "id=" + decorationId);
                return;
            }

            if (!metaProgress.IsOwned(decorationId))
            {
                CatMetaOperationResult purchase = metaProgress.TryPurchase(decorationId, ref coins);
                LogMetaEvent("decoration_purchase",
                    "id=" + decorationId
                    + " cost=" + decoration.Cost
                    + " status=" + purchase.Status
                    + " coins=" + purchase.CoinBalance);
                if (!purchase.Changed)
                {
                    ShowMetaStatus(purchase.Status == CatMetaOperationStatus.InsufficientCoins
                        ? "You need " + (decoration.Cost - coins) + " more coins."
                        : "That treasure is not available yet.");
                    return;
                }

                coins = purchase.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                metaSelectedDecorationId = decorationId;
                RenderRoomDetail(decoration.ChapterIndex);
                ShowMetaStatus("Purchased! Tap the glowing spot in the room to place it.");
                return;
            }

            if (metaProgress.IsInstalled(decorationId))
            {
                CatMetaOperationResult stored = metaProgress.Store(decorationId);
                LogMetaEvent("decoration_store", "id=" + decorationId + " status=" + stored.Status);
                if (stored.Changed)
                {
                    metaSelectedDecorationId = decorationId;
                    RenderRoomDetail(decoration.ChapterIndex);
                    ShowMetaStatus("Stored. Tap its glowing spot whenever you want it back.");
                }

                return;
            }

            metaSelectedDecorationId = decorationId;
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus("Now tap the glowing place in the room.");
        }

        private void InstallMetaDecoration(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            CatMetaOperationResult installed = metaProgress.TryInstall(decorationId);
            LogMetaEvent("decoration_install",
                "id=" + decorationId
                + " status=" + installed.Status
                + " chapterComplete=" + installed.ChapterJustCompleted);
            if (!installed.Changed)
            {
                ShowMetaStatus("That treasure could not be placed.");
                return;
            }

            metaSelectedDecorationId = null;
            PlaySfx("Snap");
            SelectFirstStoredDecoration(decoration.ChapterIndex);
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus(decoration.DisplayName + " placed • "
                + metaProgress.InstalledDecorationCount(decoration.ChapterIndex) + "/5");

            if (installed.ChapterJustCompleted)
            {
                metaRequiresRoomCompletion = false;
                ShowChapterCompletion(decoration.ChapterIndex);
            }
        }

        private void ShowFirstRunStory()
        {
            EnterMetaOverlay();
            metaHubPage.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(false);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(0);
            ShowStoryCard(
                "A HOME FOR EVERY CAT",
                "Solve cozy block puzzles to earn coins. Use each victory to turn an empty room into a safe home for a rescued cat.",
                "Meet " + chapter.CatName,
                () =>
                {
                    MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
                    ShowStoryCard(
                        "MEET " + chapter.CatName.ToUpperInvariant(),
                        chapter.StartStory + "\n\nYour first puzzle can show " + chapter.CatName + " that this home is kind.",
                        "Start Level 1",
                        CloseMetaToGameplay,
                        CatPortrait(CatMood.Neutral, chapter.CatPortraitIndex));
                },
                CatPortrait(CatMood.Happy, chapter.CatPortraitIndex));
        }

        private void ShowChapterIntro(int chapterIndex, UnityAction onDismissed)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
            ShowStoryCard(
                "MEET " + chapter.CatName.ToUpperInvariant(),
                chapter.StartStory + "\n\nComplete this room to help " + chapter.CatName + " feel at home.",
                "Begin the Chapter",
                onDismissed,
                CatPortrait(CatMood.Neutral, chapter.CatPortraitIndex));
        }





        private void EnterMetaOverlay()
        {
            if (metaOverlay == null)
            {
                return;
            }

            if (!metaOverlayOwnsPause)
            {
                bool revealWasRunning = boardRevealRoutine != null;
                metaTimerWasRunning = timerRunning || revealWasRunning;
                metaOverlayOwnsPause = true;
                if (revealWasRunning)
                {
                    StopCoroutine(boardRevealRoutine);
                    boardRevealRoutine = null;
                    for (int i = 0; i < boardRevealCells.Count; i++)
                    {
                        if (boardRevealCells[i].Rect != null)
                        {
                            boardRevealCells[i].Rect.localScale = Vector3.one;
                        }
                    }
                }
            }

            StopLevelTimer();
            StopHint();
            CancelActiveDragToRest();
            SetTrayScrollEnabled(false);
            inputLocked = true;
            GameSystem.Instance?.ShowRoomsPage();
            metaOverlay.gameObject.SetActive(true);
            metaOverlay.SetAsLastSibling();
        }

        private void PlayMetaLevel(int requestedLevelIndex)
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            if (requestedLevelIndex < 0 || requestedLevelIndex >= levelManager.LevelCount)
            {
                Debug.LogWarning("Ignoring request for a level outside the loaded catalog: " + requestedLevelIndex, this);
                return;
            }

            int safeLevel = requestedLevelIndex;
            if (metaProgress != null && !IsMetaLevelSelectable(safeLevel))
            {
                ShowMetaStatus("Clear the previous paw step first.");
                return;
            }

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            levelCompleteScreen?.Hide();

            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;
            metaRequiresRoomCompletion = false;
            GameSystem.Instance?.ShowGameplayPage();
            LogMetaEvent("level_start", "level=" + safeLevel + " chapter=" + (safeLevel / CatMetaCatalog.LevelsPerChapter));
            LoadLevel(safeLevel, true);
            ApplyMetaLevelPresentation(safeLevel);
        }

        private bool IsMetaLevelSelectable(int absoluteLevelIndex)
        {
            if (metaCatalog == null || metaProgress == null
                || absoluteLevelIndex < 0
                || levelManager == null
                || absoluteLevelIndex >= levelManager.LevelCount
                || absoluteLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return false;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(absoluteLevelIndex);
            return absoluteLevelIndex == chapter.FirstLevelIndex
                && chapter.Index == 0
                || metaProgress.IsLevelFirstCleared(absoluteLevelIndex)
                || metaProgress.IsLevelFirstCleared(absoluteLevelIndex - 1);
        }

        private int GetRecommendedLevelIndex()
        {
            int chapterIndex = Mathf.Clamp(metaProgress.HighestUnlockedChapter, 0, metaCatalog.Chapters.Count - 1);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelFirstCleared(level))
                {
                    return level;
                }
            }

            return chapter.LastLevelIndex;
        }

        private bool AreAllChapterLevelsCleared(CatMetaChapterDefinition chapter)
        {
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelFirstCleared(level))
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectFirstStoredDecoration(int chapterIndex)
        {
            if (!string.IsNullOrEmpty(metaSelectedDecorationId)
                && metaCatalog.TryGetDecoration(metaSelectedDecorationId, out CatMetaDecorationDefinition selected)
                && selected.ChapterIndex == chapterIndex
                && metaProgress.IsOwned(selected.Id)
                && !metaProgress.IsInstalled(selected.Id))
            {
                return;
            }

            metaSelectedDecorationId = null;
            IReadOnlyList<CatMetaDecorationDefinition> decorations = metaCatalog.GetChapter(chapterIndex).Decorations;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (metaProgress.IsOwned(decorations[i].Id) && !metaProgress.IsInstalled(decorations[i].Id))
                {
                    metaSelectedDecorationId = decorations[i].Id;
                    return;
                }
            }
        }

        private CatMetaDecorationDefinition FindDecorationAtMilestone(
            CatMetaChapterDefinition chapter,
            int chapterLevel)
        {
            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                if (chapter.Decorations[i].UnlockAfterChapterLevel == chapterLevel)
                {
                    return chapter.Decorations[i];
                }
            }

            return null;
        }

        private void MarkMetaStoryViewed(string storyId, int chapterIndex, string storyType)
        {
            if (metaProgress == null)
            {
                return;
            }

            CatMetaOperationResult result = metaProgress.MarkStoryViewed(storyId);
            if (result.Changed)
            {
                LogMetaEvent("story_view", "chapter=" + chapterIndex + " type=" + storyType + " id=" + storyId);
            }
        }





        private void ShowMetaStatus(string message)
        {
            if (metaStatusText != null)
            {
                metaStatusText.text = message;
            }
        }









        private void LogMetaEvent(string eventName, string values)
        {
            Debug.Log(MetaTelemetryPrefix + "event=" + eventName + " " + values);
        }
    }
}
