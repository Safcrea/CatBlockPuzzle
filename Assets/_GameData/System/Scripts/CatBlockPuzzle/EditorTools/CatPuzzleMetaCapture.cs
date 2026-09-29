#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using ActionKind = CatBlockPuzzle.CatPuzzleUiAction.ActionKind;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Renders every meta page once in Edit Mode, captures the resulting objects into the
    /// MetaProgressionView bindings, and wires every button to a persistent CatPuzzleUiAction.
    /// </summary>
    internal static class CatPuzzleMetaCapture
    {
        private static Text[] DirectTexts(Transform target) => target.Cast<Transform>()
            .Select(t => t.GetComponent<Text>()).Where(t => t != null).ToArray();

        public static void CaptureMetaScreens(CatBlockPuzzleGame game, CatPuzzleUiAuthoring ui, CatPuzzleMetaAuthoring metaUi)
        {
            MetaProgressionView view = game.MetaView;
            CatMetaCatalog metaCatalog = game.Meta.Catalog;
            metaUi.RenderRoomHubInEditor();
            view.hubStatus = view.metaStatusText;
            RectTransform metaHubPage = view.metaHubPage;
            var hubButton = metaHubPage.GetComponentsInChildren<Button>(true).Single(b => b.transform.parent == metaHubPage);
            view.hubContinueLabel = hubButton.GetComponentInChildren<Text>(true);
            BindButton(game, hubButton, ActionKind.ContinueHub);
            BindButton(game, metaHubPage.Find("Forever Home Header/Return to Puzzle").GetComponent<Button>(), ActionKind.CloseMeta);
            var chapters = metaHubPage.Find("Chapter Scroll/Chapters");
            view.hubCards = new HubCardView[10];
            for (int i = 0; i < 10; i++)
            {
                var card = chapters.Find("Chapter " + (i + 1));
                var text = DirectTexts(card);
                var badge = card.Find("State Badge");
                view.hubCards[i] = new HubCardView {
                    background = card.GetComponent<Image>(), thumbnail = card.Find("Room Thumbnail").GetComponent<Image>(),
                    number = text[0], title = text[1], progress = text[2],
                    status = badge.GetComponentInChildren<Text>(true), badge = badge.GetComponent<Image>(),
                    steps = Enumerable.Range(1, 10).Select(p => card.Find("Paw Step " + p).GetComponent<Image>()).ToArray()
                };
                BindButton(game, card.GetComponent<Button>(), ActionKind.OpenRoom, i);
            }
            var roomContainer = view.metaRoomPage;
            view.roomViews = new CatPuzzleRoomView[10];
            for (int i = 0; i < 10; i++)
            {
                var chapter = metaCatalog.GetChapter(i);
                RectTransform metaRoomPage = ui.CreatePanel(roomContainer, "Room_" + (i + 1).ToString("00"), PageColor);
                view.metaRoomPage = metaRoomPage;
                Stretch(metaRoomPage);
                metaUi.RenderRoomDetailInEditor(i);
                var roomView = metaRoomPage.gameObject.AddComponent<CatPuzzleRoomView>();
                view.roomViews[i] = roomView; roomView.chapterIndex = i;
                roomView.background = metaRoomPage.Find("Room Background").GetComponent<Image>();
                var header = metaRoomPage.Find("Room Header");
                var rail = metaRoomPage.Find("Treasure Rail");
                roomView.progress = DirectTexts(header)[1]; roomView.status = view.metaStatusText;
                roomView.levelMarkers = new Image[10];
                BindButton(game, header.Find("Back to Rooms").GetComponent<Button>(), ActionKind.Rooms);
                for (int step = 0; step < 10; step++)
                {
                    int level = chapter.FirstLevelIndex + step;
                    var marker = header.Find("Level " + (level + 1));
                    roomView.levelMarkers[step] = marker.GetComponent<Image>();
                    BindButton(game, marker.GetComponent<Button>(), ActionKind.PlayLevel, level);
                }
                roomView.cards = new CatPuzzleRoomView.DecorationCard[5];
                roomView.placements = new CatPuzzleRoomView.DecorationPlacement[5];
                for (int d = 0; d < 5; d++)
                {
                    var decoration = chapter.Decorations[d];
                    var card = rail.Find(decoration.DisplayName);
                    roomView.cards[d] = new CatPuzzleRoomView.DecorationCard {
                        id = decoration.Id, background = card.GetComponent<Image>(),
                        art = card.Find("Treasure").GetComponent<Image>(), status = DirectTexts(card)[1]
                    };
                    BindButton(game, card.GetComponent<Button>(), ActionKind.Decoration, 0, decoration.Id);
                    var placement = metaRoomPage.Find("Placed " + decoration.DisplayName);
                    roomView.placements[d] = new CatPuzzleRoomView.DecorationPlacement {
                        id = decoration.Id, root = (RectTransform)placement, background = placement.GetComponent<Image>(),
                        art = placement.Find(decoration.DisplayName + " Art").GetComponent<Image>(),
                        button = placement.GetComponent<Button>(), outline = placement.GetComponent<Outline>(), prompt = DirectTexts(placement)[0]
                    };
                    BindButton(game, placement.GetComponent<Button>(), ActionKind.Install, 0, decoration.Id);
                    placement.gameObject.SetActive(false);
                }
                roomView.gameObject.SetActive(false);
            }
            view.metaRoomPage = roomContainer;
            metaUi.ShowStoryCardInEditor("A HOME FOR EVERY CAT", "Your rescue story starts here.", "Continue", null, game.Mood.Portrait(CatMood.Happy, 0));
            view.storyPanel = (RectTransform)view.metaStoryOverlay.Find("Story Card");
            view.storyPortrait = view.storyPanel.Find("Story Cat").GetComponent<Image>();
            var storyTexts = DirectTexts(view.storyPanel); view.storyTitle = storyTexts[0]; view.storyBody = storyTexts[1];
            var storyButton = view.storyPanel.GetComponentInChildren<Button>(true);
            view.storyActionLabel = storyButton.GetComponentInChildren<Text>(true);
            BindButton(game, storyButton, ActionKind.StoryContinue);
            metaUi.ShowChapterCompletionInEditor(0);
            view.completionPanel = (RectTransform)view.metaCompletionOverlay.Find("Celebration Card");
            var completeTexts = DirectTexts(view.completionPanel);
            view.completionKicker = completeTexts[0]; view.completionTitle = completeTexts[1]; view.completionBody = completeTexts[2];
            view.completionPortrait = view.completionPanel.Find("Happy Cat").GetComponent<Image>();
            view.completionNextPreview = (RectTransform)view.completionPanel.Find("Next Room Preview");
            view.completionNextPortrait = view.completionNextPreview.Find("Next Cat").GetComponent<Image>();
            view.completionNextText = DirectTexts(view.completionNextPreview)[0];
            var completeButton = view.completionPanel.GetComponentInChildren<Button>(true);
            view.completionActionLabel = completeButton.GetComponentInChildren<Text>(true);
            BindButton(game, completeButton, ActionKind.CompletionContinue);
            view.metaCoinLabels = view.metaOverlay.GetComponentsInChildren<Text>(true).Where(t => t.transform.parent.name == "Meta Coins").ToArray();
        }

        public static void PreparePersistentControls(CatBlockPuzzleGame game)
        {
            GameplayHudView hud = game.Hud;
            MetaProgressionView view = game.MetaView;
            RectTransform gameplayScreen = hud.gameplayScreen;
            RectTransform actionBar = hud.actionBar;
            BindButton(game, gameplayScreen.Find("Rooms").GetComponent<Button>(), ActionKind.Rooms);
            BindButton(game, gameplayScreen.Find("Pause").GetComponent<Button>(), ActionKind.Pause);
            BindButton(game, gameplayScreen.Find("Settings").GetComponent<Button>(), ActionKind.Settings);
            BindButton(game, actionBar.Find("Hint").GetComponent<Button>(), ActionKind.Hint);
            BindButton(game, actionBar.Find("Reset").GetComponent<Button>(), ActionKind.Reset);
            BindButton(game, hud.previousTestButton, ActionKind.PreviousTest); BindButton(game, hud.nextTestButton, ActionKind.NextTest);
            // Reference-navigation builds omit these legacy meta controls.
            BindButton(game, view.metaNextLevelButton, ActionKind.NextLevel); BindButton(game, view.metaDecorateNowButton, ActionKind.DecorateNow);
            RectTransform failPanel = game.FailScreen.panel;
            RectTransform winPanel = game.CompleteScreen.panel;
            BindButton(game, failPanel.Find("Retry").GetComponent<Button>(), ActionKind.Reset);
            BindButton(game, failPanel.Find("Skip Level").GetComponent<Button>(), ActionKind.SkipLevel);
            BindButton(game, failPanel.Find("Home").GetComponent<Button>(), ActionKind.Home);
            BindButton(game, winPanel.Find("Continue").GetComponent<Button>(), ActionKind.NextLevel);
            foreach (var button in hud.canvas.GetComponentsInChildren<Button>(true))
                if (button.onClick.GetPersistentEventCount() != 1)
                    throw new InvalidOperationException("Button is not persistently assigned: " + button.name);
        }

        public static void BindButton(CatBlockPuzzleGame game, Button button, ActionKind action, int index = 0, string item = null)
        {
            if (button == null) return;
            var binding = button.gameObject.AddComponent<CatPuzzleUiAction>();
            binding.controller = game; binding.action = action; binding.index = index; binding.itemId = item;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, binding.Invoke);
        }

        public static void BindToggle(CatBlockPuzzleGame game, Toggle toggle, ActionKind action)
        {
            var binding = toggle.gameObject.AddComponent<CatPuzzleUiAction>();
            binding.controller = game; binding.action = action;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, binding.SetToggle);
        }
    }
}
#endif
