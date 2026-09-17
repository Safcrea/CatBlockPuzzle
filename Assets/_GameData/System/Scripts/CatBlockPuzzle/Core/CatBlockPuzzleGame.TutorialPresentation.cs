using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private Vector2 tutorialCelebrationPosition;
        private Image tutorialCoachCat;
        private readonly System.Collections.Generic.List<Image> tutorialCoachGrid = new System.Collections.Generic.List<Image>();

        private void CreateTutorialCoach()
        {
            tutorialCoach = RuntimeUiFactory.CreateRect(tutorialRoot, "Tutorial Context Card");
            RuntimeUiFactory.SetRect(tutorialCoach, Vector2.zero, new Vector2(440f, 94f));
            var panel = tutorialCoach.gameObject.AddComponent<Image>();
            panel.sprite = roundedBoxSprite;
            panel.type = Image.Type.Sliced;
            panel.color = PanelColor;
            panel.raycastTarget = false;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.25f, .15f, .1f, .18f);
            shadow.effectDistance = new Vector2(0f, -6f);
            tutorialCoachTitle = RuntimeUiFactory.CreateText(tutorialCoach, "Context Title", "", 27, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(tutorialCoachTitle.rectTransform, new Vector2(0f, 22f), new Vector2(412f, 32f));
            tutorialCoachBody = RuntimeUiFactory.CreateText(tutorialCoach, "Context Detail", "", 21, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(tutorialCoachBody.rectTransform, new Vector2(0f, -10f), new Vector2(412f, 38f));
            tutorialCoachTitle.raycastTarget = tutorialCoachBody.raycastTarget = false;
            tutorialCoachPictures = RuntimeUiFactory.CreateRect(tutorialCoach, "Visual Context");
            tutorialCoachCat = TutorialImage(tutorialCoachPictures, "Cat Group", CatPortrait(CatMood.Happy, 0), Color.white,
                new Vector2(-108f, 8f), new Vector2(53f, 53f));
            TutorialImage(tutorialCoachPictures, "Move Line", roundedBoxSprite, GoldColor,
                new Vector2(-40f, 8f), new Vector2(45f, 8f));
            var arrow = TutorialImage(tutorialCoachPictures, "Move Arrow", roundedBoxSprite, GoldColor,
                new Vector2(-17f, 15f), new Vector2(24f, 8f));
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -40f);
            arrow = TutorialImage(tutorialCoachPictures, "Move Arrow", roundedBoxSprite, GoldColor,
                new Vector2(-17f, 1f), new Vector2(24f, 8f));
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 40f);
            for (int i = 0; i < 4; i++)
                tutorialCoachGrid.Add(TutorialImage(tutorialCoachPictures, "Fill A Space", roundedBoxSprite, ValidColor,
                    new Vector2(33f + (i % 2) * 23f, 20f - (i / 2) * 23f), new Vector2(19f, 19f)));
            TutorialImage(tutorialCoachPictures, "Happy Home", pawSprite, GoldColor,
                new Vector2(110f, 8f), new Vector2(46f, 46f));
            for (int i = 0; i < 7; i++)
                tutorialProgressDots.Add(TutorialImage(tutorialCoach, "Learning Progress", circleSprite, TargetColor,
                    new Vector2((i - 3) * 32f, -35f), new Vector2(12f, 12f)));
        }

        private int TutorialProgressIndex()
        {
            switch (tutorialStep)
            {
                case TutorialStep.GoalDemo: return 0;
                case TutorialStep.FitDemo: return 1;
                case TutorialStep.EdgeDemo: return 2;
                case TutorialStep.PlaceFirst: return 3;
                case TutorialStep.ReturnFirst:
                case TutorialStep.ReplaceFirst: return 4;
                case TutorialStep.OverlapDemo: return 5;
                default: return 6;
            }
        }

        private void SetTutorialContext()
        {
            if (tutorialCoachTitle == null) return;
            string title;
            string detail;
            switch (tutorialStep)
            {
                case TutorialStep.GoalDemo:
                    title = "Give every cat a home";
                    detail = "Fill every grey space. Your timer is paused while we practise.";
                    break;
                case TutorialStep.FitDemo:
                    title = "Watch a purrfect fit";
                    detail = "Pick up the whole group, slide to its home, then let go.";
                    break;
                case TutorialStep.EdgeDemo:
                    title = "Keep the whole group inside";
                    detail = "Every cat needs a grey space. Outside the shape? It bounces back.";
                    break;
                case TutorialStep.ReturnFirst:
                    title = "You can change your mind";
                    detail = "Pick up the placed group and drag it back to the shelf.";
                    break;
                case TutorialStep.ReplaceFirst:
                    title = "Try that fit again";
                    detail = "Drag the group back into its glowing home. No penalty for practising!";
                    break;
                case TutorialStep.OverlapDemo:
                    title = "One cat per space";
                    detail = "Groups cannot overlap. Red means this spot is already taken.";
                    break;
                case TutorialStep.FillBoard:
                    title = "Finish their cosy home";
                    detail = "Fit the remaining group into the empty spaces to complete the level.";
                    break;
                default:
                    title = "Your turn!";
                    detail = "Pick up the glowing group and follow the paw trail.";
                    break;
            }
            if (tutorialCelebrationRemaining > 0f)
            { title = "Purrfect!"; detail = "That's it! You're ready for the next move."; }
            else if (drag != null && drag.Piece == tutorialPiece)
            {
                title = drag.ReturnToShelf ? "Let go to return it" : drag.Valid ? "Green means it fits!" : "Keep sliding";
                detail = drag.ReturnToShelf ? "The shelf is glowing. Release to put this group back." : drag.Valid
                    ? "Release now and the whole group will snap into place." : tutorialStep == TutorialStep.ReturnFirst
                    ? "Bring the group down to the shelf. You can always try again." : "Follow the glowing spaces. Red spots won't accept the group.";
            }
            tutorialCoachTitle.text = title;
            tutorialCoachBody.text = detail;
            tutorialCoachTitle.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachBody.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachPictures.gameObject.SetActive(!showTutorialCaptions);
            tutorialCoachPictures.localScale = new Vector3(tutorialStep == TutorialStep.ReturnFirst ? -1f : 1f, 1f, 1f);
            bool caution = tutorialStep == TutorialStep.EdgeDemo || tutorialStep == TutorialStep.OverlapDemo;
            tutorialCoachCat.sprite = CatPortrait(caution ? CatMood.Worried : CatMood.Happy, tutorialPiece.AtlasIndex);
            foreach (var space in tutorialCoachGrid) space.color = caution ? InvalidColor : ValidColor;
            int progress = TutorialProgressIndex();
            for (int i = 0; i < tutorialProgressDots.Count; i++)
            {
                tutorialProgressDots[i].color = i < progress ? TargetDeepColor : i == progress ? GoldColor : TargetColor;
                tutorialProgressDots[i].rectTransform.localScale = Vector3.one * (i == progress ? 1.3f : 1f);
            }
        }

        private void CelebrateTutorialAction(PieceState piece)
        {
            tutorialCelebrationRemaining = .7f;
            tutorialCelebrationPosition = TutorialLocal(piece.Rect.position);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, piece.Rect.position);
            SpawnSnapRing(screen, GoldColor);
            SpawnFixedBurst(screen, reducedMotion ? 2 : 8, GoldColor);
        }
    }
}
