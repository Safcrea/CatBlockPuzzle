#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using ActionKind = CatBlockPuzzle.CatPuzzleUiAction.ActionKind;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private static Text[] DirectTexts(Transform target) => target.Cast<Transform>()
            .Select(t => t.GetComponent<Text>()).Where(t => t != null).ToArray();

        private void CaptureMetaScreens()
        {
            RenderRoomHubInEditor();
            hubStatus = metaStatusText;
            var hubButton = metaHubPage.GetComponentsInChildren<Button>(true).Single(b => b.transform.parent == metaHubPage);
            hubContinueLabel = hubButton.GetComponentInChildren<Text>(true);
            BindButton(hubButton, ActionKind.ContinueHub);
            BindButton(metaHubPage.Find("Forever Home Header/Return to Puzzle").GetComponent<Button>(), ActionKind.CloseMeta);
            var chapters = metaHubPage.Find("Chapter Scroll/Chapters");
            hubCards = new HubCardView[10];
            for (int i = 0; i < 10; i++)
            {
                var card = chapters.Find("Chapter " + (i + 1));
                var text = DirectTexts(card);
                var badge = card.Find("State Badge");
                hubCards[i] = new HubCardView {
                    background = card.GetComponent<Image>(), thumbnail = card.Find("Room Thumbnail").GetComponent<Image>(),
                    number = text[0], title = text[1], progress = text[2],
                    status = badge.GetComponentInChildren<Text>(true), badge = badge.GetComponent<Image>(),
                    steps = Enumerable.Range(1,10).Select(p => card.Find("Paw Step " + p).GetComponent<Image>()).ToArray()
                };
                BindButton(card.GetComponent<Button>(), ActionKind.OpenRoom, i);
            }
            var roomContainer = metaRoomPage;
            roomViews = new CatPuzzleRoomView[10];
            for (int i = 0; i < 10; i++)
            {
                var chapter = metaCatalog.GetChapter(i);
                metaRoomPage = CreatePanel(roomContainer, "Room_" + (i + 1).ToString("00"), PageColor);
                Stretch(metaRoomPage);
                RenderRoomDetailInEditor(i);
                var view = metaRoomPage.gameObject.AddComponent<CatPuzzleRoomView>();
                roomViews[i] = view; view.chapterIndex = i;
                view.background = metaRoomPage.Find("Room Background").GetComponent<Image>();
                var header = metaRoomPage.Find("Room Header");
                var rail = metaRoomPage.Find("Treasure Rail");
                view.progress = DirectTexts(header)[1]; view.status = metaStatusText;
                view.levelMarkers = new Image[10];
                BindButton(header.Find("Back to Rooms").GetComponent<Button>(), ActionKind.Rooms);
                for (int step = 0; step < 10; step++)
                {
                    int level = chapter.FirstLevelIndex + step;
                    var marker = header.Find("Level " + (level + 1));
                    view.levelMarkers[step] = marker.GetComponent<Image>();
                    BindButton(marker.GetComponent<Button>(), ActionKind.PlayLevel, level);
                }
                view.cards = new CatPuzzleRoomView.DecorationCard[5];
                view.placements = new CatPuzzleRoomView.DecorationPlacement[5];
                for (int d = 0; d < 5; d++)
                {
                    var decoration = chapter.Decorations[d];
                    var card = rail.Find(decoration.DisplayName);
                    view.cards[d] = new CatPuzzleRoomView.DecorationCard {
                        id = decoration.Id, background = card.GetComponent<Image>(),
                        art = card.Find("Treasure").GetComponent<Image>(), status = DirectTexts(card)[1]
                    };
                    BindButton(card.GetComponent<Button>(), ActionKind.Decoration, 0, decoration.Id);
                    var placement = metaRoomPage.Find("Placed " + decoration.DisplayName);
                    view.placements[d] = new CatPuzzleRoomView.DecorationPlacement {
                        id = decoration.Id, root = (RectTransform)placement, background = placement.GetComponent<Image>(),
                        art = placement.Find(decoration.DisplayName + " Art").GetComponent<Image>(),
                        button = placement.GetComponent<Button>(), outline = placement.GetComponent<Outline>(), prompt = DirectTexts(placement)[0]
                    };
                    BindButton(placement.GetComponent<Button>(), ActionKind.Install, 0, decoration.Id);
                    placement.gameObject.SetActive(false);
                }
                view.gameObject.SetActive(false);
            }
            metaRoomPage = roomContainer;
            ShowStoryCardInEditor("A HOME FOR EVERY CAT", "Your rescue story starts here.", "Continue", null, CatPortrait(CatMood.Happy,0));
            storyPanel = (RectTransform)metaStoryOverlay.Find("Story Card");
            storyPortrait = storyPanel.Find("Story Cat").GetComponent<Image>();
            var storyTexts = DirectTexts(storyPanel); storyTitle = storyTexts[0]; storyBody = storyTexts[1];
            var storyButton = storyPanel.GetComponentInChildren<Button>(true);
            storyActionLabel = storyButton.GetComponentInChildren<Text>(true);
            BindButton(storyButton, ActionKind.StoryContinue);
            ShowChapterCompletionInEditor(0);
            completionPanel = (RectTransform)metaCompletionOverlay.Find("Celebration Card");
            var completeTexts = DirectTexts(completionPanel);
            completionKicker = completeTexts[0]; completionTitle = completeTexts[1]; completionBody = completeTexts[2];
            completionPortrait = completionPanel.Find("Happy Cat").GetComponent<Image>();
            completionNextPreview = (RectTransform)completionPanel.Find("Next Room Preview");
            completionNextPortrait = completionNextPreview.Find("Next Cat").GetComponent<Image>();
            completionNextText = DirectTexts(completionNextPreview)[0];
            var completeButton = completionPanel.GetComponentInChildren<Button>(true);
            completionActionLabel = completeButton.GetComponentInChildren<Text>(true);
            BindButton(completeButton, ActionKind.CompletionContinue);
            metaCoinLabels = metaOverlay.GetComponentsInChildren<Text>(true).Where(t => t.transform.parent.name == "Meta Coins").ToArray();
        }

        private void PreparePersistentControls()
        {
            BindButton(gameplayScreen.Find("Rooms").GetComponent<Button>(), ActionKind.Rooms);
            BindButton(gameplayScreen.Find("Pause").GetComponent<Button>(), ActionKind.Pause);
            BindButton(gameplayScreen.Find("Settings").GetComponent<Button>(), ActionKind.Settings);
            BindButton(actionBar.Find("Hint").GetComponent<Button>(), ActionKind.Hint);
            BindButton(actionBar.Find("Reset").GetComponent<Button>(), ActionKind.Reset);
            BindButton(previousTestButton, ActionKind.PreviousTest); BindButton(nextTestButton, ActionKind.NextTest);
            // Reference-navigation builds omit these legacy meta controls.
            BindButton(metaNextLevelButton, ActionKind.NextLevel); BindButton(metaDecorateNowButton, ActionKind.DecorateNow);
            BindButton(failPanel.Find("Retry").GetComponent<Button>(), ActionKind.Reset);
            BindButton(failPanel.Find("Skip Level").GetComponent<Button>(), ActionKind.SkipLevel);
            BindButton(failPanel.Find("Home").GetComponent<Button>(), ActionKind.Home);
            BindButton(winPanel.Find("Continue").GetComponent<Button>(), ActionKind.NextLevel);
            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
                if (button.onClick.GetPersistentEventCount() != 1)
                    throw new InvalidOperationException("Button is not persistently assigned: " + button.name);
        }

        private void BindButton(Button button, ActionKind action, int index = 0, string item = null)
        {
            if (button == null) return;
            var binding = button.gameObject.AddComponent<CatPuzzleUiAction>();
            binding.controller = this; binding.action = action; binding.index = index; binding.itemId = item;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, binding.Invoke);
        }
        private void BindToggle(Toggle toggle, ActionKind action)
        {
            var binding = toggle.gameObject.AddComponent<CatPuzzleUiAction>();
            binding.controller = this; binding.action = action;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, binding.SetToggle);
        }
    }
}
#endif
