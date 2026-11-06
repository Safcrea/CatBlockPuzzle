using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The Hammer power-up: one tap clears the way. It knocks the cats that sit on other cats' spots back to the shelf,
    /// breaks the padlocks that keep a cat out, and blows the fog away. Each blow is a real swing on the board. Nothing
    /// is spent unless there was something in the way.
    /// </summary>
    internal sealed class HammerController : HostedService
    {
        private const int MaxCats = 5;
        private const int MaxLocks = 3;

        private readonly PuzzleSession session;
        private readonly BoardController board;
        private readonly CellLockController locks;
        private readonly FogController fog;
        private readonly PieceDragController drag;
        private readonly PlayerSettings settings;
        private readonly FxSpawner fx;
        private readonly HapticsController haptics;
        private readonly HintPresenter hint;

        public HammerController(IGameplayContext context, PuzzleSession session, BoardController board,
            CellLockController locks, FogController fog, PieceDragController drag, PlayerSettings settings,
            FxSpawner fx, HapticsController haptics, HintPresenter hint) : base(context)
        {
            this.hint = hint;
            this.session = session;
            this.board = board;
            this.locks = locks;
            this.fog = fog;
            this.drag = drag;
            this.settings = settings;
            this.fx = fx;
            this.haptics = haptics;
        }

        private Coroutine sequence;
        private Coroutine jolt;
        private Vector2? boardRest;

        /// <summary>True while the swings are playing.</summary>
        public bool Active { get; private set; }

        /// <summary>Raised when a use starts (there was something in the way), so the power-up is spent and reported.</summary>
        public event Action Struck;

        /// <summary>True when a padlock, fog or a cat in the way gives the Hammer something to do.</summary>
        public bool HasTarget => HasObstacle;

        public bool HasObstacle => locks.HasClosedLocks || fog.CloudCount > 0 || FindBlockers().Count > 0;

        /// <summary>How many landed cats are on the wrong spot.</summary>
        public int BlockerCount => FindBlockers().Count;

        /// <summary>Swings at everything in the way. False (and nothing spent) when nothing is.</summary>
        public bool Begin()
        {
            if (Active) return false;
            List<PieceState> cats = FindBlockers();
            if (cats.Count > MaxCats) cats.RemoveRange(MaxCats, cats.Count - MaxCats);
            var lockCells = new List<Vector2Int>();
            locks.CollectClosedLockCells(lockCells);
            SortBlockingLocksFirst(lockCells);
            if (lockCells.Count > MaxLocks) lockCells.RemoveRange(MaxLocks, lockCells.Count - MaxLocks);
            var fogCells = new List<Vector2Int>();
            fog.CollectCloudCells(fogCells);
            if (cats.Count == 0 && lockCells.Count == 0 && fogCells.Count == 0) return false;

            Active = true;
            Struck?.Invoke();
            PuzzleAudio.Play(AudioEvents.HammerArm);
            Hud?.PlayPowerUpUsed(PowerUpKind.Hammer);
            sequence = StartCoroutine(Sequence(cats, lockCells, fogCells));
            return true;
        }

        /// <summary>Stops a swing in progress (a level change, a pause or leaving gameplay).</summary>
        public void Cancel()
        {
            // Stop the swings first, or the sequence carries on behind a pause screen and draws more hammers.
            StopCoroutine(sequence);
            sequence = null;
            StopCoroutine(jolt);
            jolt = null;
            if (boardRest.HasValue && session.boardRoot != null) session.boardRoot.anchoredPosition = boardRest.Value;
            boardRest = null;
            Transform swing = session.boardRoot != null ? session.boardRoot.Find("Hammer Swing") : null;
            if (swing != null) Destroy(swing.gameObject);
            if (!Active) return;
            Active = false;
            Hud?.RefreshPowerUpCounts();
        }

        // Cats of the same shape are interchangeable: one may sit on any of their authored spots.
        private static bool IsTwin(PieceState a, PieceState b) =>
            !b.Definition.IsSpare && !b.Definition.IsFixed && !a.IsRotated && !b.IsRotated && a.Solution.ShapeId == b.Solution.ShapeId;

        // A landed, unturned cat on its own authored spot or on a twin's.
        private bool OnAnAuthoredSpot(PieceState piece)
        {
            if (!piece.Placed || piece.IsRotated) return false;
            foreach (PieceState owner in session.pieces)
            {
                if (owner != piece && !IsTwin(piece, owner)) continue;
                if (piece.Row == owner.Solution.SolutionRow && piece.Col == owner.Solution.SolutionCol) return true;
            }
            return false;
        }

        // Every landed cat that is not where it belongs (fixed and spare cats never count). The ones holding another cat's spot
        // come first, since lifting them frees the way soonest.
        private List<PieceState> FindBlockers()
        {
            var wrong = new List<PieceState>();
            foreach (PieceState piece in session.pieces)
                if (piece.Placed && !piece.Definition.IsFixed && !piece.Definition.IsSpare && !OnAnAuthoredSpot(piece)) wrong.Add(piece);
            wrong.Sort((a, b) => HoldsSomeonesSpot(b).CompareTo(HoldsSomeonesSpot(a)));
            return wrong;
        }

        private int HoldsSomeonesSpot(PieceState holder)
        {
            int cells = 0;
            foreach (PieceState want in session.pieces)
            {
                if (want == holder || want.Definition.IsSpare || want.Definition.IsFixed || OnAnAuthoredSpot(want)) continue;
                foreach (CellOffset c in want.Solution.Cells)
                    if (session.occupancy.TryGetValue(new Vector2Int(want.Solution.SolutionRow + c.Row, want.Solution.SolutionCol + c.Col), out string id) &&
                        id == holder.Definition.Id) cells++;
            }
            return cells;
        }

        // Padlocks that keep a waiting cat out of its spot go first.
        private void SortBlockingLocksFirst(List<Vector2Int> cells)
        {
            var blocking = new HashSet<Vector2Int>();
            foreach (PieceState piece in session.pieces)
            {
                if (piece.Placed || piece.Definition.IsSpare) continue;
                foreach (CellOffset offset in piece.Solution.Cells)
                    blocking.Add(new Vector2Int(piece.Solution.SolutionRow + offset.Row, piece.Solution.SolutionCol + offset.Col));
            }
            cells.Sort((a, b) => (blocking.Contains(b) ? 1 : 0).CompareTo(blocking.Contains(a) ? 1 : 0));
        }

        private IEnumerator Sequence(List<PieceState> cats, List<Vector2Int> lockCells, List<Vector2Int> fogCells)
        {
            session.inputLocked = true;
            RectTransform boardRoot = session.boardRoot;
            foreach (PieceState cat in cats)
            {
                if (boardRoot == null || cat.Rect == null) continue;
                Vector2 at = boardRoot.InverseTransformPoint(cat.Rect.position);
                PieceState target = cat;
                yield return SwingAt(at, () =>
                {
                    PuzzleAudio.Play(AudioEvents.HammerBreak);
                    PuzzleAudio.Play(AudioEvents.CatSurprised);
                    session.inputLocked = false; // KnockToShelf locks input itself until the cat is home
                    drag.KnockToShelf(target);
                    session.inputLocked = true;
                });
            }
            foreach (Vector2Int cell in lockCells)
            {
                Vector2 at = board.BoardAnchoredToLocal(board.CellPosition(cell.x, cell.y));
                Vector2Int target = cell;
                yield return SwingAt(at, () =>
                {
                    locks.TryBreak(target, silent: true);
                    PuzzleAudio.Play(AudioEvents.HammerLockBreak);
                });
            }
            if (fogCells.Count > 0)
            {
                Vector2 at = board.BoardAnchoredToLocal(board.CellPosition(fogCells[0].x, fogCells[0].y));
                yield return SwingAt(at, () =>
                {
                    fog.ClearAll();
                    PuzzleAudio.Play(AudioEvents.HammerBreak);
                });
            }

            // The knocked cats are still flying home; hand the board back once they have landed.
            yield return new WaitForSecondsRealtime(settings.ReducedMotion ? 0.05f : 0.5f);
            Active = false;
            sequence = null;
            if (!session.levelFailed && !session.IsBoardFilled) session.inputLocked = false;
            Hud?.RefreshPowerUpCounts();
            // The way is clear: show what goes in next, so the player is never left wondering after a swing.
            if (!session.inputLocked) hint.ShowHint(HintReveal.FullShape);
        }

        // Rear back, strike, fade: the hammer is drawn over the target, and the effect lands on contact.
        private IEnumerator SwingAt(Vector2 at, Action impact)
        {
            RectTransform boardRoot = session.boardRoot;
            RectTransform hammer = BuildSwingHammer(boardRoot, at);
            bool animate = !settings.ReducedMotion && hammer != null;
            if (animate)
            {
                const float windUp = 0.14f, swing = 0.1f;
                float size = Mathf.Min(session.boardCellWidth, session.boardCellHeight) * 1.9f;
                hammer.sizeDelta = Vector2.one * size;
                Vector2 rest = at + new Vector2(size * 0.34f, size * 0.36f);
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
                    hammer.anchoredPosition = Vector2.Lerp(rest, at + new Vector2(size * 0.16f, size * 0.2f), ease);
                    yield return null;
                }
            }

            PuzzleAudio.Play(AudioEvents.HammerHit);
            haptics?.Play(HapticKind.LockOpen);
            if (boardRoot != null)
            {
                fx.SpawnPawBurst(boardRoot, at, settings.ReducedMotion ? 6 : 14, TargetColor);
                if (!settings.ReducedMotion)
                {
                    StopCoroutine(jolt);
                    jolt = StartCoroutine(JoltBoard(boardRoot));
                }
            }
            impact?.Invoke();

            if (animate)
            {
                float t = 0f;
                while (t < 0.18f && hammer != null)
                {
                    t += Time.unscaledDeltaTime;
                    hammer.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t / 0.18f);
                    Graphic g = hammer.GetComponent<Graphic>();
                    if (g != null) g.color = new Color(1f, 1f, 1f, 1f - t / 0.18f);
                    yield return null;
                }
            }
            if (hammer != null) Destroy(hammer.gameObject);
        }

        private RectTransform BuildSwingHammer(RectTransform boardRoot, Vector2 at)
        {
            Sprite icon = Hud != null ? Hud.HammerIcon : null;
            if (icon == null || boardRoot == null) return null;
            RectTransform rect = RuntimeUiFactory.CreateRect(boardRoot, "Hammer Swing");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.28f, 0.2f);
            rect.anchoredPosition = at;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.SetAsLastSibling();
            return rect;
        }

        private IEnumerator JoltBoard(RectTransform boardRoot)
        {
            Vector2 rest = boardRest ?? boardRoot.anchoredPosition;
            boardRest = rest;
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
            boardRest = null;
            jolt = null;
        }
    }
}
