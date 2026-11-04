using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The Hammer power-up's board mode. One use turns on the mode; the next tap on the board smashes whatever is in the
    /// way there: a padlock (it opens), fog (it clears around the tap) or a landed cat (it is knocked back to the shelf).
    /// The Hammer is only spent by a real smash, so tapping the button again, or an empty cell, costs nothing.
    /// </summary>
    internal sealed class HammerController : HostedService
    {
        private enum Target { None, Lock, Fog, Cat }

        private readonly PuzzleSession session;
        private readonly CanvasSpace space;
        private readonly BoardController board;
        private readonly CellLockController locks;
        private readonly FogController fog;
        private readonly PieceDragController drag;
        private readonly PlayerSettings settings;
        private readonly GameplayToast toast;
        private readonly FxSpawner fx;
        private readonly HapticsController haptics;

        private RectTransform catcher;
        private Coroutine pulseRoutine;
        private bool striking;
        private readonly List<RectTransform> pulseTargets = new List<RectTransform>();

        public HammerController(IGameplayContext context, PuzzleSession session, CanvasSpace space, BoardController board,
            CellLockController locks, FogController fog, PieceDragController drag, PlayerSettings settings, GameplayToast toast,
            FxSpawner fx, HapticsController haptics) : base(context)
        {
            this.session = session;
            this.space = space;
            this.board = board;
            this.locks = locks;
            this.fog = fog;
            this.drag = drag;
            this.settings = settings;
            this.toast = toast;
            this.fx = fx;
            this.haptics = haptics;
        }

        public bool Active { get; private set; }

        /// <summary>Raised after a smash landed, so the power-up is spent and reported.</summary>
        public event Action Struck;

        /// <summary>True when something on the board can be smashed, so a use would not be wasted.</summary>
        public bool HasTarget
        {
            get
            {
                if (locks.HasClosedLocks || fog.CloudCount > 0) return true;
                foreach (PieceState piece in session.pieces) if (IsSmashableCat(piece)) return true;
                return false;
            }
        }

        /// <summary>True when the lock/fog part of <see cref="HasTarget"/> is what is in the player's way.</summary>
        public bool HasObstacle => locks.HasClosedLocks || fog.CloudCount > 0;

        public void Begin()
        {
            if (Active) return;
            Active = true;
            PuzzleAudio.Play(AudioEvents.BoosterUse);
            Hud?.PlayPowerUpUsed(PowerUpKind.Hammer);
            BuildCatcher();
            toast.Show(HasObstacle ? "Tap a padlock, fog or cat\nto smash it!" : "Tap a cat on the board\nto smash it off!", 2.6f);
            Hud?.RefreshPowerUpCounts();
            if (!settings.ReducedMotion && Host.gameObject.activeInHierarchy) pulseRoutine = StartCoroutine(PulseTargets());
        }

        /// <summary>Ends the mode without a smash: the button again, a level change, a pause or leaving gameplay.</summary>
        public void Cancel()
        {
            if (!Active && catcher == null) return;
            Active = false;
            striking = false;
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
            ResetPulse();
            Transform swing = session.boardRoot != null ? session.boardRoot.Find("Hammer Swing") : null;
            if (swing != null) Destroy(swing.gameObject);
            if (catcher != null)
            {
                Destroy(catcher.gameObject);
                catcher = null;
            }
            Hud?.RefreshPowerUpCounts();
        }

        private static bool IsSmashableCat(PieceState piece) =>
            piece != null && piece.Placed && !piece.Definition.IsFixed && !piece.Definition.IsSpare;

        // A clear layer over the board catches the tap, so cats underneath are not dragged while the Hammer is armed.
        private void BuildCatcher()
        {
            RectTransform boardRoot = session.boardRoot;
            if (boardRoot == null) return;
            catcher = RuntimeUiFactory.CreateRect(boardRoot, "Hammer Catcher");
            RuntimeUiFactory.Stretch(catcher);
            catcher.offsetMin = new Vector2(-24f, -24f);
            catcher.offsetMax = new Vector2(24f, 24f);
            Image image = catcher.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;
            EventTrigger trigger = catcher.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(data => OnTap((PointerEventData)data));
            trigger.triggers.Add(entry);
            catcher.SetAsLastSibling();
        }

        private void OnTap(PointerEventData data)
        {
            if (!Active || striking || session.inputLocked || session.drag != null) return;
            RectTransform boardRoot = session.boardRoot;
            if (boardRoot == null) return;
            Vector2 local = space.ScreenToLocal(boardRoot, data.position);
            float x = local.x + session.boardWidth * 0.5f;
            float y = session.boardHeight * 0.5f - local.y;
            Vector2Int cell = new Vector2Int(
                Mathf.FloorToInt(y / (session.boardCellHeight + BoardGap)),
                Mathf.FloorToInt(x / (session.boardCellWidth + BoardGap)));
            PieceState cat = null;
            Target target = Resolve(cell, out cat);
            if (target == Target.None)
            {
                PuzzleAudio.Play(AudioEvents.LockedTap);
                toast.Show("Nothing to smash there!", 1.4f);
                return;
            }
            StartCoroutine(Strike(target, cell, cat));
        }

        private Target Resolve(Vector2Int cell, out PieceState cat)
        {
            cat = null;
            if (locks.IsLocked(cell)) return Target.Lock;
            if (session.occupancy.TryGetValue(cell, out string pieceId))
            {
                foreach (PieceState piece in session.pieces)
                {
                    if (piece.Definition.Id != pieceId) continue;
                    if (IsSmashableCat(piece)) { cat = piece; return Target.Cat; }
                    break;
                }
            }
            return fog.IsFoggy(cell) ? Target.Fog : Target.None;
        }

        // Swing in, hit, then the effect: the hammer rears back, comes down on the cell, the board jolts and the thing breaks.
        private IEnumerator Strike(Target target, Vector2Int cell, PieceState cat)
        {
            striking = true;
            session.inputLocked = true;
            RectTransform boardRoot = session.boardRoot;
            Vector2 cellLocal = board.BoardAnchoredToLocal(board.CellPosition(cell.x, cell.y));
            RectTransform hammer = BuildSwingHammer(boardRoot, cellLocal);
            bool animate = !settings.ReducedMotion && hammer != null;
            if (animate)
            {
                const float windUp = 0.16f, swing = 0.12f;
                float size = Mathf.Min(session.boardCellWidth, session.boardCellHeight) * 1.9f;
                hammer.sizeDelta = Vector2.one * size;
                Vector2 rest = cellLocal + new Vector2(size * 0.34f, size * 0.36f);
                float t = 0f;
                while (t < windUp && hammer != null)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / windUp);
                    float ease = 1f - (1f - k) * (1f - k);
                    hammer.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 55f, ease));
                    hammer.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, ease);
                    hammer.anchoredPosition = Vector2.Lerp(rest + new Vector2(size * 0.2f, size * 0.2f), rest, ease);
                    yield return null;
                }
                t = 0f;
                while (t < swing && hammer != null)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / swing);
                    float ease = k * k * k;
                    hammer.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(55f, -20f, ease));
                    hammer.anchoredPosition = Vector2.Lerp(rest, cellLocal + new Vector2(size * 0.16f, size * 0.2f), ease);
                    yield return null;
                }
            }

            PuzzleAudio.Play(AudioEvents.HammerHit);
            haptics?.Play(HapticKind.LockOpen);
            if (boardRoot != null)
            {
                fx.SpawnPawBurst(boardRoot, cellLocal, settings.ReducedMotion ? 6 : 14, TargetColor);
                if (!settings.ReducedMotion) StartCoroutine(JoltBoard(boardRoot));
            }

            switch (target)
            {
                case Target.Lock:
                    locks.TryBreak(cell);
                    break;
                case Target.Fog:
                    fog.TryClear(cell);
                    PuzzleAudio.Play(AudioEvents.HammerBreak);
                    break;
                case Target.Cat:
                    PuzzleAudio.Play(AudioEvents.HammerBreak);
                    PuzzleAudio.Play(AudioEvents.CatSurprised);
                    session.inputLocked = false; // KnockToShelf locks input itself until the cat is home
                    drag.KnockToShelf(cat);
                    break;
            }

            if (animate)
            {
                float t = 0f;
                while (t < 0.22f && hammer != null)
                {
                    t += Time.unscaledDeltaTime;
                    hammer.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t / 0.22f);
                    Graphic g = hammer.GetComponent<Graphic>();
                    if (g != null) g.color = new Color(1f, 1f, 1f, 1f - t / 0.22f);
                    yield return null;
                }
            }
            if (hammer != null) Destroy(hammer.gameObject);

            if (target != Target.Cat) session.inputLocked = false;
            Cancel();
            Struck?.Invoke();
        }

        private RectTransform BuildSwingHammer(RectTransform boardRoot, Vector2 cellLocal)
        {
            Sprite icon = Hud != null ? Hud.HammerIcon : null;
            if (icon == null || boardRoot == null) return null;
            RectTransform rect = RuntimeUiFactory.CreateRect(boardRoot, "Hammer Swing");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.28f, 0.2f);
            rect.anchoredPosition = cellLocal;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.SetAsLastSibling();
            return rect;
        }

        private IEnumerator JoltBoard(RectTransform boardRoot)
        {
            Vector2 rest = boardRoot.anchoredPosition;
            float t = 0f;
            const float seconds = 0.2f;
            while (t < seconds && boardRoot != null)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / seconds);
                boardRoot.anchoredPosition = rest + new Vector2(Mathf.Sin(t * 90f), Mathf.Cos(t * 70f)) * (9f * k);
                yield return null;
            }
            if (boardRoot != null) boardRoot.anchoredPosition = rest;
        }

        // While the Hammer is armed everything it can smash gently pulses, so the player sees where to tap.
        private IEnumerator PulseTargets()
        {
            float elapsed = 0f;
            while (Active)
            {
                elapsed += Time.unscaledDeltaTime;
                if (!striking)
                {
                    pulseTargets.Clear();
                    locks.CollectClosedLockRects(pulseTargets);
                    fog.CollectCloudRects(pulseTargets);
                    foreach (PieceState piece in session.pieces) if (IsSmashableCat(piece) && piece.Rect != null) pulseTargets.Add(piece.Rect);
                    float scale = 1f + 0.06f * (Mathf.Sin(elapsed * 7f) * 0.5f + 0.5f);
                    foreach (RectTransform target in pulseTargets) if (target != null) target.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }

        private void ResetPulse()
        {
            foreach (RectTransform target in pulseTargets) if (target != null) target.localScale = Vector3.one;
            pulseTargets.Clear();
        }
    }
}
