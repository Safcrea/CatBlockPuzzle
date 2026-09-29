using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleInteractionTests : PreparedScenePlayModeFixture
    {
        private static object Member(object target, string name) => target.GetType().GetField(name).GetValue(target);

        [UnityTest]
        public IEnumerator PlacedCard_HidesGrowsRemainingAndReturnsWithBoardPiece()
        {
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(0.8f);

            IList pieces = (IList)Field("pieces");
            Assert.That(pieces, Is.Not.Null);
            Assert.That(pieces.Count, Is.GreaterThanOrEqualTo(2));

            object placedState = pieces[0];
            object remainingState = pieces[1];
            RectTransform placedSlot = (RectTransform)Member(placedState, "Slot");
            RectTransform placedRect = (RectTransform)Member(placedState, "Rect");
            float remainingCellBefore = (float)Member(remainingState, "CellWidth");

            object definition = Member(placedState, "Definition");
            int solutionRow = (int)Member(definition, "SolutionRow");
            int solutionCol = (int)Member(definition, "SolutionCol");
            Call("PlacePiece", placedState, solutionRow, solutionCol, false);
            yield return null;
            Assert.That(
                Mathf.Abs(placedRect.localScale.x - placedRect.localScale.y),
                Is.GreaterThan(0.001f),
                "Successful placement did not trigger the landing squish.");
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.That(placedSlot.gameObject.activeSelf, Is.False, "Placed piece left an empty tray card visible.");
            float remainingCellAfter = (float)Member(remainingState, "CellWidth");
            Assert.That(remainingCellAfter, Is.GreaterThan(remainingCellBefore), "Remaining tray cats did not grow.");

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 17,
                position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, placedRect.position)
            };
            bool began = (bool)Call("BeginPieceDrag", placedState, pointer);
            Assert.That(began, Is.True);
            Assert.That(placedSlot.gameObject.activeSelf, Is.True, "Picking up a board group did not restore its tray card.");

            RectTransform tray = FindRect(canvas.transform, "Shelf");
            pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, tray.position);
            Call("DragPiece", placedState, pointer);
            Call("EndPieceDrag", placedState, pointer);
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(placedSlot.gameObject.activeSelf, Is.True);
            Assert.That((bool)Member(placedState, "Placed"), Is.False);
            Assert.That(placedRect.parent, Is.EqualTo(placedSlot));
        }

        [UnityTest]
        public IEnumerator DraggedPiece_UsesFreeTargetWithDelayedJellyMotion()
        {
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(0.8f);

            IList pieces = (IList)Field("pieces");
            Assert.That(pieces, Is.Not.Null.And.Not.Empty);
            object state = pieces[0];
            RectTransform rect = (RectTransform)Member(state, "Rect");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 23,
                position = startScreen
            };

            bool began = (bool)Call("BeginPieceDrag", state, pointer);
            Assert.That(began, Is.True);
            RectTransform fxLayer = (RectTransform)Field("fxLayer");
            int activePickupFx = 0;
            for (int i = 0; i < fxLayer.childCount; i++)
            {
                if (fxLayer.GetChild(i).gameObject.activeSelf)
                {
                    activePickupFx++;
                }
            }

            Assert.That(activePickupFx, Is.GreaterThan(0), "Picking a tray piece did not spawn visual feedback.");
            float horizontalMove = startScreen.x < Screen.width * 0.5f ? 280f : -280f;
            pointer.position = startScreen + new Vector2(horizontalMove, 170f);
            Call("DragPiece", state, pointer);

            // The session's DragState (not the drag controller) carries the live pointer anchor and target.
            object dragState = Field("drag");
            Vector2 anchor = (Vector2)Member(dragState, "LastAnchorScreen");
            Vector2 target = (Vector2)Member(dragState, "TargetPosition");
            object grab = Member(dragState, "Grabbed");
            Vector2 expectedFreeTarget = (Vector2)Call("PieceFreeCenterRoot",
                anchor,
                state,
                grab,
                Member(state, "CellWidth"),
                Member(state, "CellHeight"),
                Member(state, "GapX"),
                Member(state, "GapY"));

            Assert.That(Vector2.Distance(target, expectedFreeTarget), Is.LessThan(0.05f), "The held piece target was still forced to a board cell.");
            yield return null;

            dragState = Field("drag");
            Vector2 updatedTarget = (Vector2)Member(dragState, "TargetPosition");
            Assert.That(Vector2.Distance(rect.anchoredPosition, updatedTarget), Is.GreaterThan(0.5f), "The held piece did not retain the configured follow delay.");

            yield return null;
            yield return null;
            float jellyDifference = Mathf.Abs(rect.localScale.x - rect.localScale.y);
            float tilt = Quaternion.Angle(Quaternion.identity, rect.localRotation);
            Assert.That(jellyDifference > 0.0005f || tilt > 0.1f, Is.True, "The held piece did not show tilt or directional squash/stretch.");

            Call("EndPieceDrag", state, pointer);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        [UnityTest]
        public IEnumerator ThemePaletteChangesAtFiveLevels_AndRoomBackgroundChangesAtTen()
        {
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(0.15f);
            Image background = (Image)Field("backgroundImage");
            Image tray = (Image)Field("trayImage");
            Text objective = (Text)Field("objectiveText");
            int firstTheme = (int)Field("activeThemeIndex");
            Sprite firstSprite = background.sprite;
            Color firstTrayColor = tray.color;
            Assert.That(firstTheme, Is.EqualTo(0));
            Assert.That(firstSprite.texture.name, Is.EqualTo("room_01"));
            Assert.That(objective.text, Does.Contain("Welcome Nook"));

            Call("PreviewLevelForTesting", 4);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That((int)Field("activeThemeIndex"), Is.EqualTo(firstTheme));
            Assert.That(background.sprite, Is.SameAs(firstSprite));

            Call("PreviewLevelForTesting", 5);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That((int)Field("activeThemeIndex"), Is.EqualTo(1));
            Assert.That(background.sprite, Is.SameAs(firstSprite), "A room background should span its ten-level chapter.");
            // Every level now owns a saved tray; read the active level's assigned image.
            tray = (Image)Field("trayImage");
            Assert.That(tray.color, Is.Not.EqualTo(firstTrayColor));
            Assert.That(objective.text, Does.Contain("Welcome Nook"));

            Call("PreviewLevelForTesting", 10);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(background.sprite, Is.Not.SameAs(firstSprite));
            Assert.That(background.sprite.texture.name, Is.EqualTo("room_02"));
            Assert.That(objective.text, Does.Contain("Maker Corner"));
        }

        private static RectTransform FindRect(Transform parent, string name)
        {
            RectTransform[] children = parent.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }
    }
}
