using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private const string MovePieceTipSeenKey = "CatBlockPuzzle.Tutorial.MovePiece.v1";
        private RectTransform movePieceTipRoot;
        private RectTransform movePieceTipBubble;
        private Coroutine movePieceTipRoutine;
        private PieceState movePieceTipTarget;

        // Level 1's walkthrough no longer teaches "you can move a placed piece" (see
        // CatBlockPuzzleGame.Tutorial.cs). Instead, the first time any level lets a player
        // validly place a piece somewhere other than its own solution cell, show a small
        // animated hand carrying it over to where it belongs, once, ever.
        private void TryShowMovePieceTip(PieceState state)
        {
            if (IsLevelOneTutorialOpen || inputLocked) return;
            if (state.Row == state.Definition.SolutionRow && state.Col == state.Definition.SolutionCol) return;
            if (PlayerPrefs.GetInt(MovePieceTipSeenKey, 0) != 0) return;
            if (!levelNavigationTesting)
            {
                PlayerPrefs.SetInt(MovePieceTipSeenKey, 1);
                PlayerPrefs.Save();
            }
            StopHint();
            ShowMovePieceTip(state);
        }

        private void ShowMovePieceTip(PieceState state)
        {
            if (levelRoot == null) return;
            movePieceTipTarget = state;
            movePieceTipRoot = RuntimeUiFactory.CreateRect(levelRoot, "Move Piece Tip");
            RuntimeUiFactory.Stretch(movePieceTipRoot);
            var group = movePieceTipRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var halo = TutorialImage(movePieceTipRoot, "Destination Halo", circleSprite, GoldColor, Vector2.zero, new Vector2(90f, 90f));

            var hand = RuntimeUiFactory.CreateRect(movePieceTipRoot, "Move Piece Hand");
            TutorialImage(hand, "Palm Outline", roundedBoxSprite, InkColor, new Vector2(15f, -38f), new Vector2(52f, 60f));
            TutorialImage(hand, "Palm", roundedBoxSprite, PanelColor, new Vector2(15f, -37f), new Vector2(43f, 51f));
            TutorialImage(hand, "Finger Outline", roundedBoxSprite, InkColor, new Vector2(0f, -13f), new Vector2(23f, 57f));
            TutorialImage(hand, "Finger", roundedBoxSprite, PanelColor, new Vector2(0f, -13f), new Vector2(15f, 49f));
            TutorialImage(hand, "Thumb", roundedBoxSprite, PanelColor, new Vector2(37f, -30f), new Vector2(17f, 35f));

            ShowMovePieceTipBubble(state);
            movePieceTipRoutine = StartCoroutine(MovePieceTipRoutine(group, halo.rectTransform, hand, state));
        }

        private void ShowMovePieceTipBubble(PieceState state)
        {
            RectTransform bubble = RuntimeUiFactory.CreateRect(movePieceTipRoot, "Move Piece Tip Caption");
            RuntimeUiFactory.SetRect(bubble, Vector2.zero, new Vector2(380f, 78f));
            var panel = bubble.gameObject.AddComponent<Image>();
            panel.sprite = roundedBoxSprite;
            panel.type = Image.Type.Sliced;
            panel.color = PanelColor;
            panel.raycastTarget = false;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.25f, .15f, .1f, .18f);
            shadow.effectDistance = new Vector2(0f, -6f);
            var text = RuntimeUiFactory.CreateText(bubble, "Message",
                "Psst! Placed cats can move too - drag it to the glowing spot.", 22, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(text.rectTransform, Vector2.zero, new Vector2(348f, 66f));
            text.raycastTarget = false;
            bubble.anchoredPosition = ScreenToLocal(movePieceTipRoot,
                BoardPieceCenterScreen(state, state.Row, state.Col)) + new Vector2(0f, 90f);
            movePieceTipBubble = bubble;
        }

        private IEnumerator MovePieceTipRoutine(CanvasGroup group, RectTransform halo, RectTransform hand, PieceState state)
        {
            Vector2 startScreen = BoardPieceCenterScreen(state, state.Row, state.Col);
            Vector2 destScreen = BoardPieceCenterScreen(state, state.Definition.SolutionRow, state.Definition.SolutionCol);
            halo.anchoredPosition = ScreenToLocal(movePieceTipRoot, destScreen);
            hand.anchoredPosition = ScreenToLocal(movePieceTipRoot, startScreen);

            const float fadeIn = .2f, grab = .35f, travel = .9f, release = .45f, fadeOut = .4f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = reducedMotion ? 1f : Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            group.alpha = 1f;

            // A grab pulse on the hand and the real piece, so it is obvious what is about to move.
            t = 0f;
            while (t < grab)
            {
                t += Time.unscaledDeltaTime;
                float wave = reducedMotion ? 0f : Mathf.Sin(Mathf.Clamp01(t / grab) * Mathf.PI);
                hand.localScale = Vector3.one * (1f - .08f * wave);
                if (state.Rect != null) state.Rect.localScale = Vector3.one * (1f + .06f * wave);
                yield return null;
            }
            if (state.Rect != null) state.Rect.localScale = Vector3.one;

            t = 0f;
            while (t < travel)
            {
                t += Time.unscaledDeltaTime;
                float eased = reducedMotion ? 1f : EaseOutCubic(Mathf.Clamp01(t / travel));
                hand.anchoredPosition = ScreenToLocal(movePieceTipRoot, Vector2.Lerp(startScreen, destScreen, eased));
                float pulse = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(t * 6f);
                halo.localScale = Vector3.one * (1f + .15f * pulse);
                yield return null;
            }
            hand.anchoredPosition = ScreenToLocal(movePieceTipRoot, destScreen);

            // A release pulse at the destination, mirroring the grab.
            t = 0f;
            while (t < release)
            {
                t += Time.unscaledDeltaTime;
                float wave = reducedMotion ? 0f : Mathf.Sin(Mathf.Clamp01(t / release) * Mathf.PI);
                hand.localScale = Vector3.one * (1f - .08f * wave);
                yield return null;
            }

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = reducedMotion ? 0f : 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            StopMovePieceTip();
        }

        private Vector2 ScreenToLocal(RectTransform root, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, canvas.worldCamera, out Vector2 local);
            return local;
        }

        private void StopMovePieceTip()
        {
            if (movePieceTipRoutine != null) { StopCoroutine(movePieceTipRoutine); movePieceTipRoutine = null; }
            if (movePieceTipTarget != null && movePieceTipTarget.Rect != null) movePieceTipTarget.Rect.localScale = Vector3.one;
            movePieceTipTarget = null;
            if (movePieceTipRoot != null) { movePieceTipRoot.gameObject.SetActive(false); Destroy(movePieceTipRoot.gameObject); }
            movePieceTipRoot = null;
            movePieceTipBubble = null;
        }
    }
}
