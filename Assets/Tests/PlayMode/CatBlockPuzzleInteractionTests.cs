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
