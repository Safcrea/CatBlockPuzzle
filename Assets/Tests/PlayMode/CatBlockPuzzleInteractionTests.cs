using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleInteractionTests
    {
        [UnityTest]
        public IEnumerator PlacedCard_HidesGrowsRemainingAndReturnsWithBoardPiece()
        {
            MonoBehaviour game = null;
            for (int i = 0; i < 120 && game == null; i++)
            {
                MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    if (behaviours[b].GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame")
                    {
                        game = behaviours[b];
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(game, Is.Not.Null);
            game.GetType().GetMethod("PreviewLevelForTesting", BindingFlags.Instance | BindingFlags.Public).Invoke(game, new object[] { 0 });
            yield return new WaitForSecondsRealtime(0.8f);

            System.Type gameType = game.GetType();
            FieldInfo piecesField = gameType.GetField("pieces", BindingFlags.Instance | BindingFlags.NonPublic);
            IList pieces = piecesField.GetValue(game) as IList;
            Assert.That(pieces, Is.Not.Null);
            Assert.That(pieces.Count, Is.GreaterThanOrEqualTo(2));

            object placedState = pieces[0];
            object remainingState = pieces[1];
            System.Type stateType = placedState.GetType();
            RectTransform placedSlot = (RectTransform)stateType.GetField("Slot").GetValue(placedState);
            RectTransform placedRect = (RectTransform)stateType.GetField("Rect").GetValue(placedState);
            float remainingCellBefore = (float)stateType.GetField("CellWidth").GetValue(remainingState);

            object definition = stateType.GetField("Definition").GetValue(placedState);
            System.Type definitionType = definition.GetType();
            int solutionRow = (int)definitionType.GetField("SolutionRow").GetValue(definition);
            int solutionCol = (int)definitionType.GetField("SolutionCol").GetValue(definition);
            MethodInfo placePiece = FindMethod(gameType, "PlacePiece");
            placePiece.Invoke(game, new[] { placedState, (object)solutionRow, solutionCol, false });
            yield return null;
            Assert.That(
                Mathf.Abs(placedRect.localScale.x - placedRect.localScale.y),
                Is.GreaterThan(0.001f),
                "Successful placement did not trigger the landing squish.");
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.That(placedSlot.gameObject.activeSelf, Is.False, "Placed piece left an empty tray card visible.");
            float remainingCellAfter = (float)stateType.GetField("CellWidth").GetValue(remainingState);
            Assert.That(remainingCellAfter, Is.GreaterThan(remainingCellBefore), "Remaining tray cats did not grow.");

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 17,
                position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, placedRect.position)
            };
            MethodInfo beginDrag = FindMethod(gameType, "BeginPieceDrag");
            bool began = (bool)beginDrag.Invoke(game, new[] { placedState, pointer });
            Assert.That(began, Is.True);
            Assert.That(placedSlot.gameObject.activeSelf, Is.True, "Picking up a board group did not restore its tray card.");

            RectTransform tray = FindRect(canvas.transform, "Shelf");
            pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, tray.position);
            FindMethod(gameType, "DragPiece").Invoke(game, new[] { placedState, pointer });
            FindMethod(gameType, "EndPieceDrag").Invoke(game, new[] { placedState, pointer });
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(placedSlot.gameObject.activeSelf, Is.True);
            Assert.That((bool)stateType.GetField("Placed").GetValue(placedState), Is.False);
            Assert.That(placedRect.parent, Is.EqualTo(placedSlot));
        }

        [UnityTest]
        public IEnumerator DraggedPiece_UsesFreeTargetWithDelayedJellyMotion()
        {
            MonoBehaviour game = null;
            for (int i = 0; i < 120 && game == null; i++)
            {
                MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    if (behaviours[b].GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame")
                    {
                        game = behaviours[b];
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(game, Is.Not.Null);
            System.Type gameType = game.GetType();
            gameType.GetMethod("PreviewLevelForTesting", BindingFlags.Instance | BindingFlags.Public).Invoke(game, new object[] { 0 });
            yield return new WaitForSecondsRealtime(0.8f);

            IList pieces = gameType.GetField("pieces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game) as IList;
            Assert.That(pieces, Is.Not.Null.And.Not.Empty);
            object state = pieces[0];
            System.Type stateType = state.GetType();
            RectTransform rect = (RectTransform)stateType.GetField("Rect").GetValue(state);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 23,
                position = startScreen
            };

            bool began = (bool)FindMethod(gameType, "BeginPieceDrag").Invoke(game, new[] { state, pointer });
            Assert.That(began, Is.True);
            RectTransform fxLayer = (RectTransform)gameType.GetField("fxLayer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
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
            FindMethod(gameType, "DragPiece").Invoke(game, new[] { state, pointer });

            object dragState = gameType.GetField("drag", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            System.Type dragType = dragState.GetType();
            Vector2 anchor = (Vector2)dragType.GetField("LastAnchorScreen").GetValue(dragState);
            Vector2 target = (Vector2)dragType.GetField("TargetPosition").GetValue(dragState);
            object grab = dragType.GetField("Grabbed").GetValue(dragState);
            Vector2 expectedFreeTarget = (Vector2)FindMethod(gameType, "PieceFreeCenterRoot").Invoke(game, new[]
            {
                (object)anchor,
                state,
                grab,
                stateType.GetField("CellWidth").GetValue(state),
                stateType.GetField("CellHeight").GetValue(state),
                stateType.GetField("GapX").GetValue(state),
                stateType.GetField("GapY").GetValue(state)
            });

            Assert.That(Vector2.Distance(target, expectedFreeTarget), Is.LessThan(0.05f), "The held piece target was still forced to a board cell.");
            yield return null;

            dragState = gameType.GetField("drag", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            Vector2 updatedTarget = (Vector2)dragType.GetField("TargetPosition").GetValue(dragState);
            Assert.That(Vector2.Distance(rect.anchoredPosition, updatedTarget), Is.GreaterThan(0.5f), "The held piece did not retain the configured follow delay.");

            yield return null;
            yield return null;
            float jellyDifference = Mathf.Abs(rect.localScale.x - rect.localScale.y);
            float tilt = Quaternion.Angle(Quaternion.identity, rect.localRotation);
            Assert.That(jellyDifference > 0.0005f || tilt > 0.1f, Is.True, "The held piece did not show tilt or directional squash/stretch.");

            FindMethod(gameType, "EndPieceDrag").Invoke(game, new[] { state, pointer });
            yield return new WaitForSecondsRealtime(0.4f);
        }

        [UnityTest]
        public IEnumerator Theme_ChangesAtFiveLevelBoundariesAndUsesAtlasPalette()
        {
            MonoBehaviour game = null;
            for (int i = 0; i < 120 && game == null; i++)
            {
                MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    if (behaviours[b].GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame")
                    {
                        game = behaviours[b];
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(game, Is.Not.Null);
            System.Type gameType = game.GetType();
            MethodInfo previewLevel = gameType.GetMethod("PreviewLevelForTesting", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo themeIndexField = gameType.GetField("activeThemeIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo backgroundField = gameType.GetField("backgroundImage", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo trayField = gameType.GetField("trayImage", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo objectiveField = gameType.GetField("objectiveText", BindingFlags.Instance | BindingFlags.NonPublic);

            previewLevel.Invoke(game, new object[] { 0 });
            yield return new WaitForSecondsRealtime(0.15f);
            Image background = (Image)backgroundField.GetValue(game);
            Image tray = (Image)trayField.GetValue(game);
            Text objective = (Text)objectiveField.GetValue(game);
            int firstTheme = (int)themeIndexField.GetValue(game);
            Sprite firstSprite = background.sprite;
            Color firstTrayColor = tray.color;
            Assert.That(firstTheme, Is.EqualTo(0));
            Assert.That(firstSprite.texture.name, Is.EqualTo("theme_atlas"));
            Assert.That(objective.text, Does.Contain("Sunlit Glasshouse"));

            previewLevel.Invoke(game, new object[] { 4 });
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That((int)themeIndexField.GetValue(game), Is.EqualTo(firstTheme));
            Assert.That(background.sprite, Is.SameAs(firstSprite));

            previewLevel.Invoke(game, new object[] { 5 });
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That((int)themeIndexField.GetValue(game), Is.EqualTo(1));
            Assert.That(background.sprite, Is.Not.SameAs(firstSprite));
            Assert.That(background.sprite.rect.x, Is.GreaterThan(firstSprite.rect.x));
            Assert.That(tray.color, Is.Not.EqualTo(firstTrayColor));
            Assert.That(objective.text, Does.Contain("Sugar Patisserie"));
        }

        private static MethodInfo FindMethod(System.Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing runtime method " + name + ".");
            return method;
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
