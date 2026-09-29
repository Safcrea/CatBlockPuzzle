using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>Counts consecutive first-try placements and drives the "PURRFECT" badge.</summary>
    internal sealed class ComboTracker : HostedService
    {
        private readonly PuzzleSession session;
        private readonly FxSpawner fx;
        private readonly CanvasSpace space;
        private readonly PlayerSettings settings;

        public ComboTracker(IGameplayContext context, PuzzleSession session, FxSpawner fx, CanvasSpace space, PlayerSettings settings) : base(context)
        {
            this.session = session;
            this.fx = fx;
            this.space = space;
            this.settings = settings;
        }

        private RectTransform comboBadge => Hud.comboBadge;
        private Text comboText => Hud.comboText;

        public void Register()
        {
            session.comboCount++;
            session.bestCombo = Mathf.Max(session.bestCombo, session.comboCount);
            UpdateDisplay(true);
            int comboCount = session.comboCount;
            if ((comboCount == 3 || comboCount == 5 || comboCount == 8) && comboBadge != null)
            {
                Vector2 screen = space.WorldToScreen(comboBadge.position);
                fx.SpawnFixedBurst(screen, settings.ReducedMotion ? 4 : 12, comboCount >= 5 ? GoldColor : ValidColor);
            }
        }

        public void Reset()
        {
            session.comboCount = 0;
            UpdateDisplay(false);
        }

        public void UpdateDisplay(bool animate)
        {
            if (comboBadge == null || comboText == null)
            {
                return;
            }

            bool visible = session.comboCount >= 3;
            comboBadge.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            comboText.text = session.comboCount + "x  PURRFECT";
            if (animate && !settings.ReducedMotion)
            {
                StartCoroutine(fx.PopTransform(comboBadge, 1.1f));
            }
        }
    }
}
