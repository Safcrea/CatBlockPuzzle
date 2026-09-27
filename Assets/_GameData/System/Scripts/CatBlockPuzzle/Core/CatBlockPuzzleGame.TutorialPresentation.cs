using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private Vector2 tutorialCelebrationPosition;
        private Image tutorialCoachCat;
        private readonly System.Collections.Generic.List<Image> tutorialCoachGrid = new System.Collections.Generic.List<Image>();
        private const int TutorialProgressStepCount = 2;

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
            for (int i = 0; i < TutorialProgressStepCount; i++)
                tutorialProgressDots.Add(TutorialImage(tutorialCoach, "Learning Progress", circleSprite, TargetColor,
                    new Vector2((i - (TutorialProgressStepCount - 1) * .5f) * 32f, -35f), new Vector2(12f, 12f)));
        }

        private int TutorialProgressIndex() => tutorialStep == TutorialStep.PlaceFirst ? 0 : 1;

        private void SetTutorialContext()
        {
            if (tutorialCoachTitle == null) return;
            string title;
            string detail;
            if (tutorialStep == TutorialStep.PlaceFirst)
            {
                title = "Give this cat a home";
                detail = "Pick up the glowing group and drag it into its matching space.";
            }
            else
            {
                title = "Now the second one";
                detail = "Pick up this glowing group and drag it into its matching space.";
            }
            if (tutorialCelebrationRemaining > 0f)
            { title = "Purrfect!"; detail = "That's it! You're ready for the next move."; }
            else if (drag != null && drag.Piece == tutorialPiece)
            {
                title = drag.Valid ? "Green means it fits!" : "Keep sliding";
                detail = drag.Valid ? "Release now and the whole group will snap into place." : "Follow the glowing spaces. Red spots won't accept the group.";
            }
            tutorialCoachTitle.text = title;
            tutorialCoachBody.text = detail;
            tutorialCoachTitle.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachBody.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachPictures.gameObject.SetActive(!showTutorialCaptions);
            tutorialCoachCat.sprite = CatPortrait(CatMood.Happy, tutorialPiece.AtlasIndex);
            foreach (var space in tutorialCoachGrid) space.color = ValidColor;
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
