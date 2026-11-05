using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>Pulses the next misplaced piece and its solution cells for a moment.</summary>
    internal sealed class HintPresenter : HostedService
    {
        private readonly PuzzleSession session;
        private readonly BoardController board;
        private readonly MovePieceTip tip;
        private readonly CellLockController locks;
        private readonly FxSpawner fx;
        private const float MaxUnlockWaitSeconds = 1.5f;
        private Coroutine hintRoutine;
        private Coroutine waitRoutine;
        private PieceState lastHinted;

        public HintPresenter(IGameplayContext context, PuzzleSession session, BoardController board, MovePieceTip tip, CellLockController locks, FxSpawner fx) : base(context)
        {
            this.fx = fx;
            this.locks = locks;
            this.session = session;
            this.board = board;
            this.tip = tip;
        }

        private List<PieceState> pieces => session.pieces;

        public void ShowHint() => ShowHint(HintReveal.FullShape);

        /// <summary>Pulses the next cat to place and its spot; false when no cat could be pointed at.</summary>
        public bool ShowHint(HintReveal reveal)
        {
            if (session.inputLocked)
            {
                // A cat flying back to the shelf after a refused drop locks input for a moment; the hint waits for it
                // instead of silently doing nothing. A finished level never gets one.
                if (session.levelFailed || session.IsBoardFilled) return false;
                if (waitRoutine == null) waitRoutine = StartCoroutine(ShowWhenUnlocked(reveal));
                return true;
            }

            StopHint();
            PieceState hinted = PickHintTarget(out List<PieceState> blockers, out PieceState spot);
            if (hinted != null)
            {
                lastHinted = hinted;
                PuzzleAudio.Play(AudioEvents.HintShow);
                SparkleSpot(spot);
                hintRoutine = StartCoroutine(HintRoutine(hinted, spot, reveal, blockers));
            }
            return hinted != null;
        }

        private IEnumerator ShowWhenUnlocked(HintReveal reveal)
        {
            float waited = 0f;
            while (session.inputLocked && waited < MaxUnlockWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            waitRoutine = null;
            if (!session.inputLocked) ShowHint(reveal);
        }

        // A few gold sparks pop off the spot the hint points at, so the eye lands there before the pulse starts.
        private void SparkleSpot(PieceState state)
        {
            RectTransform boardRoot = session.boardRoot;
            if (boardRoot == null || state.Solution.Cells.Length == 0) return;
            CellOffset first = state.Solution.Cells[0];
            Vector2 local = board.BoardAnchoredToLocal(board.CellPosition(state.Solution.SolutionRow + first.Row, state.Solution.SolutionCol + first.Col));
            fx.SpawnPawBurst(boardRoot, local, 6, GoldColor);
        }

        // A hint is only useful when its spot can take the cat right now, so spots already covered by another cat
        // (the player may have filled the board a different valid way) are skipped. When several cats qualify the
        // hint moves on to the next one instead of repeating the previous cat every time.
        // Cats of the same shape are interchangeable: any of them may sit on any of those authored spots, so a spot
        // taken by a twin never makes the other twin's hint wrong.
        private PieceState PickHintTarget(out List<PieceState> blockers, out PieceState spot)
        {
            blockers = null;
            spot = null;
            List<PieceState> fitting = new List<PieceState>();
            List<PieceState> fittingSpots = new List<PieceState>();
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state.Placed || state.Definition.IsSpare || state.Definition.IsFixed) continue;
                if (state.Slot != null && !state.Slot.gameObject.activeSelf) continue;
                PieceState freeSpot = FreeSpotFor(state);
                if (freeSpot == null) continue;
                fitting.Add(state);
                fittingSpots.Add(freeSpot);
            }

            if (fitting.Count > 0)
            {
                int after = lastHinted != null ? pieces.IndexOf(lastHinted) : -1;
                int pick = 0;
                for (int i = 0; i < fitting.Count; i++)
                {
                    if (pieces.IndexOf(fitting[i]) > after) { pick = i; break; }
                }
                spot = fittingSpots[pick];
                return fitting[pick];
            }

            // Nothing left fits an authored spot: point at a placed cat that is in the way of its own spot.
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (!state.Placed || state.Definition.IsSpare || state.Definition.IsFixed) continue;
                if (SittingOnAuthoredSpot(state)) continue;
                if (SolutionSpotIsFree(state, state)) { spot = state; return state; }
            }

            // Near the end every spot can be held by a cat that landed in the wrong place. A hint must still say something:
            // show the next cat's spot and pulse the cats sitting on it, so the player knows what to move.
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state.Placed || state.Definition.IsSpare || state.Definition.IsFixed) continue;
                if (state.Slot != null && !state.Slot.gameObject.activeSelf) continue;
                List<PieceState> holding = PiecesOnSpot(state);
                if (holding.Count > 0) { blockers = holding; spot = state; return state; }
            }

            // Everything is on the board but a cat is off its own spot: point at that cat and where it belongs.
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (!state.Placed || state.Definition.IsSpare || state.Definition.IsFixed) continue;
                if (!SittingOnAuthoredSpot(state)) { spot = state; return state; }
            }
            return null;
        }

        // The cat's own authored spot when it is free, else a free spot authored for a twin (same shape), else null.
        private PieceState FreeSpotFor(PieceState state)
        {
            if (SolutionSpotIsFree(state)) return state;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState twin = pieces[i];
                if (twin == state || !IsTwin(state, twin)) continue;
                if (SolutionSpotIsFree(state, null, twin)) return twin;
            }
            return null;
        }

        private static bool IsTwin(PieceState a, PieceState b) =>
            !b.Definition.IsSpare && !b.Definition.IsFixed && !a.IsRotated && !b.IsRotated && a.Solution.ShapeId == b.Solution.ShapeId;

        // A landed, unturned cat that sits exactly on its own authored spot or on a twin's.
        private bool SittingOnAuthoredSpot(PieceState state)
        {
            if (state.IsRotated) return false;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState owner = pieces[i];
                if (owner != state && !IsTwin(state, owner)) continue;
                if (state.Row == owner.Solution.SolutionRow && state.Col == owner.Solution.SolutionCol) return true;
            }
            return false;
        }

        // The landed cats that sit on any cell of this cat's spot.
        private List<PieceState> PiecesOnSpot(PieceState state)
        {
            var found = new List<PieceState>();
            CellOffset[] cells = state.Solution.Cells;
            for (int c = 0; c < cells.Length; c++)
            {
                Vector2Int coord = new Vector2Int(state.Solution.SolutionRow + cells[c].Row, state.Solution.SolutionCol + cells[c].Col);
                if (!session.occupancy.TryGetValue(coord, out string holderId)) continue;
                for (int i = 0; i < pieces.Count; i++)
                    if (pieces[i] != state && pieces[i].Placed && pieces[i].Definition.Id == holderId && !found.Contains(pieces[i])) found.Add(pieces[i]);
            }
            return found;
        }

        // spotOwner is the cat whose authored spot is checked; it defaults to the cat itself.
        private bool SolutionSpotIsFree(PieceState state, PieceState ignoring = null, PieceState spotOwner = null)
        {
            PieceState owner = spotOwner ?? state;
            CellOffset[] cells = owner.Solution.Cells;
            for (int c = 0; c < cells.Length; c++)
            {
                Vector2Int coord = new Vector2Int(owner.Solution.SolutionRow + cells[c].Row, owner.Solution.SolutionCol + cells[c].Col);
                if (board.IsCellFree(coord, state)) continue;
                if (ignoring != null && OccupiedOnlyBy(ignoring, coord)) continue;
                return false;
            }
            return true;
        }

        // True when the cell is held by this placed cat and by nothing else, so moving that cat would free it.
        private static bool OccupiedOnlyBy(PieceState state, Vector2Int coord)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                if (coord == new Vector2Int(state.Row + state.Definition.Cells[i].Row, state.Col + state.Definition.Cells[i].Col)) return true;
            }
            return false;
        }

        private IEnumerator HintRoutine(PieceState state, PieceState spot, HintReveal reveal, List<PieceState> blockers)
        {
            List<Image> hintedCells = new List<Image>();
            CellOffset[] cells = spot.Solution.Cells;
            int revealed = reveal == HintReveal.AnchorOnly ? Mathf.Min(1, cells.Length) : cells.Length;
            for (int c = 0; c < revealed; c++)
            {
                CellOffset cell = cells[c];
                Vector2Int coord = new Vector2Int(spot.Solution.SolutionRow + cell.Row, spot.Solution.SolutionCol + cell.Col);
                if (session.boardCells.TryGetValue(coord, out CellView cellView))
                {
                    hintedCells.Add(cellView.Image);
                }
            }

            float elapsed = 0f;
            while (elapsed < 1.7f)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(elapsed * 11f) * 0.5f + 0.5f;
                state.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, wave);
                if (blockers != null)
                    for (int i = 0; i < blockers.Count; i++)
                        if (blockers[i].Rect != null) blockers[i].Rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, wave);
                state.SlotImage.color = Color.Lerp(TraySlotSkin.RestColor, new Color(1f, 0.92f, 0.55f, 0.92f), wave);
                for (int i = 0; i < hintedCells.Count; i++)
                {
                    hintedCells[i].color = Color.Lerp(TargetColor, GoldColor, wave);
                }

                yield return null;
            }

            state.Rect.localScale = Vector3.one;
            if (blockers != null)
                for (int i = 0; i < blockers.Count; i++)
                    if (blockers[i].Rect != null) blockers[i].Rect.localScale = Vector3.one;
            state.SlotImage.color = state.Placed ? TraySlotSkin.DimColor : TraySlotSkin.RestColor;
            for (int i = 0; i < hintedCells.Count; i++)
            {
                hintedCells[i].color = TargetColor;
            }

            hintRoutine = null;
        }

        /// <summary>Ends the hint pulse and the move-piece tip, restoring every piece and cell.</summary>
        public void StopHint()
        {
            tip.Stop();
            if (waitRoutine != null)
            {
                StopCoroutine(waitRoutine);
                waitRoutine = null;
            }

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].Rect.localScale = Vector3.one;
                pieces[i].SlotImage.color = pieces[i].Placed ? TraySlotSkin.DimColor : TraySlotSkin.RestColor;
            }

            board.ResetCellColors();
        }
    }
}
