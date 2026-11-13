using System;
using System.Collections.Generic;
using AVNEvents;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Undo: takes back the player's last cat moves, newest first. Each attempt at a level has a few free undos; after
    /// that each one costs coins. A Hammer swing clears the history, because it moves cats the player did not move.
    /// </summary>
    internal sealed class UndoController
    {
        public const int FreePerLevel = 3;
        public const int CoinPrice = 20;

        private readonly struct Move
        {
            public readonly PieceState Piece;
            public readonly bool WasPlaced;
            public readonly int Row, Col;
            public readonly int NewRow, NewCol;
            public Move(PieceState piece, bool wasPlaced, int row, int col)
            {
                Piece = piece; WasPlaced = wasPlaced; Row = row; Col = col; NewRow = piece.Row; NewCol = piece.Col;
            }
        }

        private readonly PuzzleSession session;
        private readonly PieceDragController drag;
        private readonly PlayerProgress progress;
        private readonly List<Move> history = new List<Move>();
        private int freeLeft = FreePerLevel;
        private int serial = -1;

        /// <summary>The count or price the button shows changed.</summary>
        public event Action Changed;

        public UndoController(PuzzleSession session, PieceDragController drag, PlayerProgress progress, HammerController hammer)
        {
            this.session = session;
            this.drag = drag;
            this.progress = progress;
            drag.PiecePlaced += Record;
            drag.PieceMoved += Remember;
            if (hammer != null) hammer.Struck += () => { SyncAttempt(); history.Clear(); Changed?.Invoke(); };
        }

        public int FreeLeft { get { SyncAttempt(); return freeLeft; } }
        public bool HasMove { get { SyncAttempt(); return history.Count > 0; } }

        public enum Result { Undone, NothingToUndo, NotEnoughCoins, Busy }

        public Result TryUndo()
        {
            SyncAttempt();
            // Drop moves whose cat has since been moved by something else (a knock, a Hammer, a Rotate).
            while (history.Count > 0)
            {
                Move last = history[history.Count - 1];
                if (last.Piece != null && last.Piece.Placed && last.Piece.Row == last.NewRow && last.Piece.Col == last.NewCol) break;
                history.RemoveAt(history.Count - 1);
            }
            if (history.Count == 0) return Result.NothingToUndo;
            bool free = freeLeft > 0;
            if (!free && progress.Coins < CoinPrice) return Result.NotEnoughCoins;
            Move move = history[history.Count - 1];
            if (!drag.UndoMove(move.Piece, move.WasPlaced, move.Row, move.Col)) return Result.Busy;
            history.RemoveAt(history.Count - 1);
            if (free) freeLeft--;
            else
            {
                progress.Coins -= CoinPrice;
                progress.SaveCoins();
                AvnEvents.EconomySpent(CoinPrice, progress.Coins, EconomySource.powerup, EconomyDetail.undo);
            }
            session.stats.undos++;
            Changed?.Invoke();
            return Result.Undone;
        }

        private Move pending;
        private bool hasPending;

        // The drag reports the old spot just before the cat lands; the landing then completes the record.
        private void Remember(PieceState piece, bool wasPlaced, int row, int col)
        {
            pending = new Move(piece, wasPlaced, row, col);
            hasPending = true;
        }

        private void Record(PieceState piece)
        {
            SyncAttempt();
            if (!hasPending || pending.Piece != piece) return;
            hasPending = false;
            history.Add(new Move(piece, pending.WasPlaced, pending.Row, pending.Col));
            Changed?.Invoke();
        }

        // A new level or a restart starts a fresh attempt: empty history, all free undos back.
        private void SyncAttempt()
        {
            if (serial == session.loadSerial) return;
            serial = session.loadSerial;
            history.Clear();
            hasPending = false;
            freeLeft = FreePerLevel;
            Changed?.Invoke();
        }
    }
}
