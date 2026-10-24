using System.Collections;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The Rotate power-up's tray mode. One use turns on the mode; the first shelf cat the player
    /// taps is turned 90 degrees clockwise, and further taps on that same cat keep turning it. The
    /// mode ends when that cat lands on the board (or the level is left). Winning still only needs
    /// a full board, so a turned cat that fills the board wins legitimately.
    /// </summary>
    internal sealed class RotateController : HostedService
    {
        private readonly PuzzleSession session;
        private readonly TrayLayout tray;
        private readonly PlayerSettings settings;
        private readonly GameplayToast toast;
        private PieceState chosen;

        public RotateController(IGameplayContext context, PuzzleSession session, TrayLayout tray, PieceDragController drag,
            PlayerSettings settings, GameplayToast toast) : base(context)
        {
            this.session = session;
            this.tray = tray;
            this.settings = settings;
            this.toast = toast;
            drag.PieceTapped += OnPieceTapped;
            drag.PiecePlaced += OnPiecePlaced;
        }

        public bool Active { get; private set; }
        /// <summary>The cat this use is turning, once one has been tapped.</summary>
        public PieceState Chosen => chosen;

        /// <summary>True when there is a shelf cat to turn, so a use would not be wasted.</summary>
        public bool HasTurnableCat
        {
            get
            {
                foreach (PieceState piece in session.pieces)
                    if (CanTurn(piece)) return true;
                return false;
            }
        }

        public void Begin()
        {
            Active = true;
            chosen = null;
            toast.Show("Tap a cat on the shelf to turn it!", 2.4f);
            Hud?.RefreshPowerUpCounts();
        }

        /// <summary>Ends the mode without a drop: a level change, restart or leaving gameplay.</summary>
        public void Cancel()
        {
            if (!Active) return;
            Active = false;
            chosen = null;
            Hud?.RefreshPowerUpCounts();
        }

        /// <summary>Turns a shelf cat; the first one tapped in this use is the only one that turns.</summary>
        public bool TryTurn(PieceState piece)
        {
            if (!Active || !CanTurn(piece) || (chosen != null && chosen != piece)) return false;
            chosen = piece;
            piece.RotateClockwise();
            tray.RelayoutTrayPiece(piece);
            PuzzleAudio.Play("Snap");
            if (!settings.ReducedMotion && Host.gameObject.activeInHierarchy && piece.Slot != null) StartCoroutine(Spin(piece));
            return true;
        }

        private static bool CanTurn(PieceState piece) =>
            piece != null && !piece.Placed && !piece.Definition.IsFixed && piece.Slot != null && piece.Slot.gameObject.activeInHierarchy;

        private void OnPieceTapped(PieceState piece)
        {
            if (!Active) return;
            if (!TryTurn(piece) && chosen != null && piece != chosen) toast.Show("This Rotate is for the cat you turned.", 1.6f);
        }

        private void OnPiecePlaced(PieceState piece)
        {
            if (Active && piece == chosen) Cancel();
        }

        // The card swings from a quarter turn back to rest, so the new shape reads as a clockwise turn.
        private IEnumerator Spin(PieceState piece)
        {
            RectTransform card = piece.Slot;
            const float seconds = 0.22f;
            float elapsed = 0f;
            while (elapsed < seconds && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float eased = 1f - (1f - t) * (1f - t);
                card.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(90f, 0f, eased));
                card.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            if (card == null) yield break;
            card.localEulerAngles = Vector3.zero;
            card.localScale = Vector3.one;
        }
    }
}
