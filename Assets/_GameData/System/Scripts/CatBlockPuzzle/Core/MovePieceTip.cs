using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>
    /// A one-time hint that placed cats can be moved: the first time a valid drop lands
    /// somewhere other than its solution cell, an animated hand carries it to the glowing
    /// spot where it belongs.
    /// </summary>
    internal sealed class MovePieceTip : HostedService
    {
        private readonly PuzzleSession session;
        private readonly CanvasSpace space;
        private readonly BoardController board;
        private readonly PlayerSettings settings;

        private RectTransform movePieceTipRoot;
        private RectTransform movePieceTipBubble;
        private Coroutine movePieceTipRoutine;
        private PieceState movePieceTipTarget;

        public MovePieceTip(IGameplayContext context, PuzzleSession session, CanvasSpace space, BoardController board, PlayerSettings settings) : base(context)
        {
            this.session = session;
            this.space = space;
            this.board = board;
            this.settings = settings;
        }

        private bool reducedMotion => settings.ReducedMotion;

        /// <summary>
        /// True when this placement should trigger the tip. Consumes the once-ever flag
        /// (except during level previews) so the caller can go straight to <see cref="Show"/>.
        /// </summary>
        public bool TryClaimShow(PieceState state, bool onboardingOpen)
        {
            if (onboardingOpen || session.inputLocked) return false;
            if (state.Row == state.Definition.SolutionRow && state.Col == state.Definition.SolutionCol) return false;
            if (PlayerPrefs.GetInt(PuzzleSaveKeys.MovePieceTipSeen, 0) != 0) return false;
            if (!session.levelNavigationTesting)
            {
                PlayerPrefs.SetInt(PuzzleSaveKeys.MovePieceTipSeen, 1);
                PlayerPrefs.Save();
            }
            return true;
        }

        public void Show(PieceState state)
        {
            RectTransform levelRoot = Hud.levelRoot;
            if (levelRoot == null) return;
            movePieceTipTarget = state;
            movePieceTipRoot = RuntimeUiFactory.CreateRect(levelRoot, "Move Piece Tip");
            RuntimeUiFactory.Stretch(movePieceTipRoot);
            var group = movePieceTipRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var halo = RuntimeUiFactory.CreateImage(movePieceTipRoot, "Destination Halo", Assets.circleSprite, GoldColor, Vector2.zero, new Vector2(90f, 90f));

            Sprite roundedBoxSprite = Assets.roundedBoxSprite;
            var hand = RuntimeUiFactory.CreateRect(movePieceTipRoot, "Move Piece Hand");
            RuntimeUiFactory.CreateImage(hand, "Palm Outline", roundedBoxSprite, InkColor, new Vector2(15f, -38f), new Vector2(52f, 60f));
            RuntimeUiFactory.CreateImage(hand, "Palm", roundedBoxSprite, PanelColor, new Vector2(15f, -37f), new Vector2(43f, 51f));
            RuntimeUiFactory.CreateImage(hand, "Finger Outline", roundedBoxSprite, InkColor, new Vector2(0f, -13f), new Vector2(23f, 57f));
            RuntimeUiFactory.CreateImage(hand, "Finger", roundedBoxSprite, PanelColor, new Vector2(0f, -13f), new Vector2(15f, 49f));
            RuntimeUiFactory.CreateImage(hand, "Thumb", roundedBoxSprite, PanelColor, new Vector2(37f, -30f), new Vector2(17f, 35f));

            ShowBubble(state);
            movePieceTipRoutine = StartCoroutine(Routine(group, halo.rectTransform, hand, state));
        }

        private void ShowBubble(PieceState state)
        {
            RectTransform bubble = RuntimeUiFactory.CreateRect(movePieceTipRoot, "Move Piece Tip Caption");
            RuntimeUiFactory.SetRect(bubble, Vector2.zero, new Vector2(380f, 78f));
            var panel = bubble.gameObject.AddComponent<Image>();
            panel.sprite = Assets.roundedBoxSprite;
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
            bubble.anchoredPosition = space.ScreenToLocal(movePieceTipRoot,
                board.BoardPieceCenterScreen(state, state.Row, state.Col)) + new Vector2(0f, 90f);
            movePieceTipBubble = bubble;
        }

        private IEnumerator Routine(CanvasGroup group, RectTransform halo, RectTransform hand, PieceState state)
        {
            Vector2 startScreen = board.BoardPieceCenterScreen(state, state.Row, state.Col);
            Vector2 destScreen = board.BoardPieceCenterScreen(state, state.Definition.SolutionRow, state.Definition.SolutionCol);
            halo.anchoredPosition = space.ScreenToLocal(movePieceTipRoot, destScreen);
            hand.anchoredPosition = space.ScreenToLocal(movePieceTipRoot, startScreen);

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
                hand.anchoredPosition = space.ScreenToLocal(movePieceTipRoot, Vector2.Lerp(startScreen, destScreen, eased));
                float pulse = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(t * 6f);
                halo.localScale = Vector3.one * (1f + .15f * pulse);
                yield return null;
            }
            hand.anchoredPosition = space.ScreenToLocal(movePieceTipRoot, destScreen);

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
            Stop();
        }

        public void Stop()
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
