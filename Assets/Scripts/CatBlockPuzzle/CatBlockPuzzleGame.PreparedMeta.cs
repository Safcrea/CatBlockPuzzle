using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        [Serializable]
        private sealed class HubCardView
        {
            public Image background;
            public Image thumbnail;
            public Text number;
            public Text title;
            public Text progress;
            public Text status;
            public Image badge;
            public Image[] steps;
        }
        [SerializeField] private HubCardView[] hubCards;
        [SerializeField] private CatPuzzleRoomView[] roomViews;
        [SerializeField] private Text hubContinueLabel;
        [SerializeField] private Text hubStatus;
        [SerializeField] private Text[] metaCoinLabels;
        [SerializeField] private RectTransform storyPanel;
        [SerializeField] private Image storyPortrait;
        [SerializeField] private Text storyTitle;
        [SerializeField] private Text storyBody;
        [SerializeField] private Text storyActionLabel;
        [SerializeField] private RectTransform completionPanel;
        [SerializeField] private Text completionKicker;
        [SerializeField] private Text completionTitle;
        [SerializeField] private Image completionPortrait;
        [SerializeField] private Text completionBody;
        [SerializeField] private RectTransform completionNextPreview;
        [SerializeField] private Image completionNextPortrait;
        [SerializeField] private Text completionNextText;
        [SerializeField] private Text completionActionLabel;
        private UnityAction pendingStoryAction;
        private int completionChapterIndex;
        private Coroutine backgroundFadeRoutine;

        private bool ValidatePreparedMeta(out string error)
        {
            error = null;
            if (hubCards == null || hubCards.Length != 10 || roomViews == null || roomViews.Length != 10 ||
                hubContinueLabel == null || hubStatus == null || metaCoinLabels == null || metaCoinLabels.Length != 11 ||
                storyPanel == null || storyPortrait == null || storyTitle == null || storyBody == null || storyActionLabel == null ||
                completionPanel == null || completionKicker == null || completionTitle == null || completionPortrait == null ||
                completionBody == null || completionNextPreview == null || completionNextPortrait == null ||
                completionNextText == null || completionActionLabel == null)
            { error = "prepared room or modal references are incomplete"; return false; }
            foreach (var coin in metaCoinLabels) if (coin == null) { error = "a coin label is missing"; return false; }
            for (int i = 0; i < 10; i++)
            {
                var hub = hubCards[i]; var room = roomViews[i];
                if (hub == null || hub.background == null || hub.thumbnail == null || hub.title == null ||
                    hub.number == null || hub.progress == null || hub.status == null || hub.badge == null || hub.steps.Length != 10 ||
                    room == null || room.chapterIndex != i || room.background == null || room.background.sprite == null ||
                    room.progress == null || room.status == null || room.levelMarkers.Length != 10 || room.cards.Length != 5 || room.placements.Length != 5)
                { error = "room " + (i + 1) + " is incomplete"; return false; }
                foreach (var card in room.cards) if (card == null || card.background == null || card.art == null || card.art.sprite == null || card.status == null)
                { error = "a decoration card is incomplete in room " + (i + 1); return false; }
                foreach (var target in room.placements) if (target == null || target.root == null || target.background == null || target.art == null || target.button == null || target.outline == null || target.prompt == null)
                { error = "a decoration target is incomplete in room " + (i + 1); return false; }
            }
            return true;
        }

        private void RefreshCoinLabels()
        {
            coinText.text = coins.ToString();
            foreach (var label in metaCoinLabels) label.text = coins.ToString();
        }

        private Sprite PreparedDecorationSprite(string id)
        {
            foreach (var item in contentCatalog.decorations) if (item.id == id) return item.sprite;
            return null;
        }

        private void RenderRoomHub()
        {
            foreach (var room in roomViews) room.gameObject.SetActive(false);
            metaStatusText = hubStatus;
            hubStatus.text = "Choose a room or continue your rescue story.";
            RefreshCoinLabels();
            for (int i = 0; i < hubCards.Length; i++)
            {
                var view = hubCards[i]; var chapter = metaCatalog.GetChapter(i);
                bool unlocked = metaProgress.IsChapterUnlocked(i), complete = metaProgress.IsChapterComplete(i);
                int installed = metaProgress.InstalledDecorationCount(i);
                view.background.color = unlocked ? new Color(1f,.98f,.93f,.99f) : new Color(.82f,.8f,.77f,.98f);
                view.thumbnail.color = unlocked ? Color.white : new Color(.42f,.42f,.42f,.78f);
                view.number.color = complete ? TargetDeepColor : SoftInkColor;
                view.title.text = unlocked ? chapter.Title : "Locked Room";
                view.progress.text = unlocked ? chapter.CatName + "  •  " + installed + "/5 treasures" : "Finish Room " + i;
                view.progress.color = unlocked ? SoftInkColor : new Color(.42f,.4f,.38f,1f);
                view.status.text = complete ? "COMPLETE" : unlocked ? installed + "/5" : "LOCKED";
                view.badge.color = complete ? TargetDeepColor : unlocked ? new Color(.96f,.56f,.48f,.96f) : new Color(.38f,.36f,.35f,.85f);
                for (int p = 0; p < 10; p++) view.steps[p].color = MetaStepColor(chapter.FirstLevelIndex + p);
            }
            int next = GetRecommendedLevelIndex();
            var recommended = metaCatalog.GetChapterForLevel(next);
            bool decorating = AreAllChapterLevelsCleared(recommended) && !metaProgress.IsChapterComplete(recommended.Index);
            hubContinueLabel.text = decorating ? "Finish " + recommended.Title : "Continue • Level " + (next + 1);
        }

        private Color MetaStepColor(int level) => metaProgress.IsLevelFirstCleared(level) ? GoldColor :
            IsMetaLevelSelectable(level) ? TargetDeepColor : new Color(.54f,.5f,.46f,.32f);

        private void RenderRoomDetail(int chapterIndex)
        {
            for (int i = 0; i < roomViews.Length; i++) roomViews[i].gameObject.SetActive(i == chapterIndex);
            var view = roomViews[chapterIndex]; var chapter = metaCatalog.GetChapter(chapterIndex);
            metaStatusText = view.status;
            view.status.text = "Buy a treasure, then tap its glowing place in the room.";
            view.progress.text = chapter.CatName + "  •  " + metaProgress.InstalledDecorationCount(chapterIndex) + "/5 placed";
            RefreshCoinLabels();
            for (int i = 0; i < 10; i++) view.levelMarkers[i].color = MetaStepColor(chapter.FirstLevelIndex + i);
            foreach (var card in view.cards)
            {
                metaCatalog.TryGetDecoration(card.id, out var decoration);
                bool unlocked = metaProgress.IsDecorationUnlocked(card.id), owned = metaProgress.IsOwned(card.id), installed = metaProgress.IsInstalled(card.id);
                bool selected = owned && !installed && metaSelectedDecorationId == card.id;
                card.background.color = selected ? new Color(1f,.86f,.52f,1f) : installed ? new Color(.72f,.9f,.75f,.99f) :
                    unlocked ? new Color(1f,.98f,.93f,.99f) : new Color(.78f,.76f,.73f,.96f);
                card.art.color = owned ? Color.white : unlocked ? new Color(.27f,.24f,.22f,.28f) : new Color(.25f,.25f,.25f,.2f);
                card.status.text = !unlocked ? "CLEAR " + decoration.UnlockAfterChapterLevel : !owned ? decoration.Cost + " COINS" : installed ? "STORE" : selected ? "TAP GLOW" : "SELECT";
                card.status.color = !unlocked ? SoftInkColor : !owned ? new Color(.76f,.47f,.08f) : selected ? new Color(.72f,.43f,.05f) : TargetDeepColor;
            }
            foreach (var placement in view.placements)
            {
                bool installed = metaProgress.IsInstalled(placement.id);
                bool selected = metaProgress.IsOwned(placement.id) && !installed && metaSelectedDecorationId == placement.id;
                placement.root.gameObject.SetActive(installed || selected);
                placement.background.color = selected ? new Color(1f,.83f,.3f,.34f) : Color.clear;
                placement.background.raycastTarget = selected;
                placement.art.color = selected ? new Color(1f,1f,1f,.58f) : Color.white;
                placement.button.interactable = selected;
                placement.outline.enabled = selected;
                placement.prompt.gameObject.SetActive(selected);
            }
        }

        private void ShowStoryCard(string titleValue, string bodyValue, string actionLabel, UnityAction action, Sprite portraitSprite)
        {
            EnterMetaOverlay();
            storyTitle.text = titleValue; storyBody.text = bodyValue; storyActionLabel.text = actionLabel;
            storyPortrait.sprite = portraitSprite != null ? portraitSprite : catHeadSprite;
            pendingStoryAction = action;
            metaStoryOverlay.gameObject.SetActive(true);
            metaStoryOverlay.SetAsLastSibling();
            storyPanel.localScale = reducedMotion ? Vector3.one : Vector3.one * .9f;
            if (!reducedMotion) StartCoroutine(PopTransform(storyPanel, 1.025f));
        }

        private void ShowChapterCompletion(int chapterIndex)
        {
            completionChapterIndex = chapterIndex;
            var chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Completion(chapter.Id), chapter.Index, "completion");
            bool final = chapterIndex == roomViews.Length - 1;
            completionKicker.text = final ? "FOREVER HOME COMPLETE" : "ROOM COMPLETE";
            completionTitle.text = chapter.Title; completionBody.text = chapter.CompletionStory;
            completionPortrait.sprite = CatPortrait(CatMood.Happy, chapter.CatPortraitIndex);
            completionNextPreview.gameObject.SetActive(!final);
            if (!final)
            {
                var next = metaCatalog.GetChapter(chapterIndex + 1);
                completionNextPortrait.sprite = CatPortrait(CatMood.Neutral, next.CatPortraitIndex);
                completionNextText.text = "NEXT: " + next.Title + "\nMeet " + next.CatName;
            }
            completionActionLabel.text = final ? "See the Forever Home" : "Meet the Next Cat";
            metaCompletionOverlay.gameObject.SetActive(true);
            metaCompletionOverlay.SetAsLastSibling();
            completionPanel.localScale = reducedMotion ? Vector3.one : Vector3.one * .88f;
            if (!reducedMotion) StartCoroutine(PopTransform(completionPanel, 1.035f));
            LogMetaEvent("room_complete", "chapter=" + chapterIndex + " id=" + chapter.Id);
        }

        private void CrossfadeGameplayBackground(Sprite nextSprite)
        {
            if (nextSprite == null) return;
            if (backgroundFadeRoutine != null) StopCoroutine(backgroundFadeRoutine);
            var previous = backgroundImage.sprite;
            backgroundImage.sprite = nextSprite; backgroundImage.color = Color.white;
            backgroundCrossfade.gameObject.SetActive(false);
            if (reducedMotion || previous == null || previous == nextSprite) return;
            backgroundCrossfade.sprite = previous; backgroundCrossfade.color = Color.white;
            backgroundCrossfade.gameObject.SetActive(true);
            backgroundFadeRoutine = StartCoroutine(FadePreparedBackground());
        }
        private IEnumerator FadePreparedBackground()
        {
            float elapsed = 0;
            while (elapsed < .35f)
            {
                elapsed += Time.unscaledDeltaTime;
                backgroundCrossfade.color = new Color(1,1,1,1-Mathf.Clamp01(elapsed/.35f));
                yield return null;
            }
            backgroundCrossfade.gameObject.SetActive(false);
            backgroundFadeRoutine = null;
        }

        public void HandlePreparedAction(CatPuzzleUiAction.ActionKind action, int index, string itemId)
        {
            PlayButtonSound();
            switch (action)
            {
                case CatPuzzleUiAction.ActionKind.Rooms: OpenRoomHub(); break;
                case CatPuzzleUiAction.ActionKind.Pause: OpenPause(); break;
                case CatPuzzleUiAction.ActionKind.Settings: OpenSettings(); break;
                case CatPuzzleUiAction.ActionKind.Hint: ShowHint(); break;
                case CatPuzzleUiAction.ActionKind.Reset: ResetLevel(); break;
                case CatPuzzleUiAction.ActionKind.PreviousTest: LoadPreviousTestLevel(); break;
                case CatPuzzleUiAction.ActionKind.NextTest: LoadNextTestLevel(); break;
                case CatPuzzleUiAction.ActionKind.NextLevel: LoadNextLevelThroughMetaGate(); break;
                case CatPuzzleUiAction.ActionKind.CloseSettings: CloseSettings(); break;
                case CatPuzzleUiAction.ActionKind.CloseMeta: CloseMetaToGameplay(); break;
                case CatPuzzleUiAction.ActionKind.ContinueHub:
                    int next = GetRecommendedLevelIndex(); var chapter = metaCatalog.GetChapterForLevel(next);
                    if (AreAllChapterLevelsCleared(chapter) && !metaProgress.IsChapterComplete(chapter.Index)) OpenRoomDetail(chapter.Index);
                    else PlayMetaLevel(next);
                    break;
                case CatPuzzleUiAction.ActionKind.OpenRoom:
                    if (metaProgress.IsChapterUnlocked(index)) OpenRoomDetail(index);
                    else ShowMetaStatus("Complete the room before this one to unlock it.");
                    break;
                case CatPuzzleUiAction.ActionKind.PlayLevel:
                    if (IsMetaLevelSelectable(index)) PlayMetaLevel(index); else ShowMetaStatus("Clear the previous paw step first.");
                    break;
                case CatPuzzleUiAction.ActionKind.Decoration: OnDecorationCardPressed(itemId); break;
                case CatPuzzleUiAction.ActionKind.Install: InstallMetaDecoration(itemId); break;
                case CatPuzzleUiAction.ActionKind.DecorateNow:
                    winOverlay.gameObject.SetActive(false); OpenRoomDetail(metaCatalog.GetChapterForLevel(levelIndex).Index); break;
                case CatPuzzleUiAction.ActionKind.StoryContinue:
                    metaStoryOverlay.gameObject.SetActive(false);
                    var pending = pendingStoryAction; pendingStoryAction = null; pending?.Invoke(); break;
                case CatPuzzleUiAction.ActionKind.CompletionContinue:
                    metaCompletionOverlay.gameObject.SetActive(false);
                    if (completionChapterIndex == roomViews.Length - 1) OpenRoomHub();
                    else { int following = completionChapterIndex + 1; ShowChapterIntro(following, () => PlayMetaLevel(metaCatalog.GetChapter(following).FirstLevelIndex)); }
                    break;
            }
        }

        public void HandlePreparedToggle(CatPuzzleUiAction.ActionKind action, bool value)
        {
            if (action == CatPuzzleUiAction.ActionKind.Sound) SetSound(value);
            else if (action == CatPuzzleUiAction.ActionKind.Haptics) SetHaptics(value);
            else if (action == CatPuzzleUiAction.ActionKind.Motion) SetReducedMotion(value);
        }
    }
}
