using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Move-limit stars: on a level with a move limit, stars come from drops instead of the
    /// timer. A drop is one release of a cat that lands on the board, including moving a cat
    /// already on the board. The "Drops 3/10" label takes the progress stars' place.
    /// </summary>
    internal sealed class DropCounter : HostedService
    {
        private const string LabelName = "Drops Counter";

        private readonly PuzzleSession session;
        private Text label;

        public DropCounter(IGameplayContext context, PuzzleSession session) : base(context)
        {
            this.session = session;
        }

        private LevelDefinition activeLevel => session.activeLevel;
        public bool Active => activeLevel != null && activeLevel.HasMoves;

        /// <summary>Shows the counter (and hides the stars) on move-limit levels; restores the stars elsewhere.</summary>
        public void ResetForLevel()
        {
            session.drops = 0;
            RectTransform stars = Hud != null ? Hud.starPanel : null;
            if (stars != null) stars.gameObject.SetActive(!Active);
            if (!Active)
            {
                if (label != null) label.gameObject.SetActive(false);
                return;
            }

            EnsureLabel(stars);
            Refresh();
        }

        public void RecordDrop()
        {
            if (!Active) return;
            session.drops++;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            label.text = "Drops " + session.drops + "/" + activeLevel.Moves;
            // Past the three-star limit the count turns warm, like the timer's warning colour.
            label.color = session.drops > activeLevel.Moves ? PuzzleStyle.TimerWarningColor : PuzzleStyle.TimerNormalColor;
            label.gameObject.SetActive(true);
        }

        private void EnsureLabel(RectTransform stars)
        {
            if (label != null || stars == null) return;
            Transform existing = stars.parent.Find(LabelName);
            if (existing != null) { label = existing.GetComponent<Text>(); return; }

            Text timer = Hud.timerText;
            label = RuntimeUiFactory.CreateText((RectTransform)stars.parent, LabelName, "", timer != null ? timer.fontSize : 40, TextAnchor.MiddleCenter);
            if (timer != null) label.font = timer.font;
            else if (Assets.defaultFont != null) label.font = Assets.defaultFont;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = timer != null ? timer.fontSize : 40;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = stars.anchorMin;
            rect.anchorMax = stars.anchorMax;
            rect.pivot = stars.pivot;
            rect.anchoredPosition = stars.anchoredPosition;
            rect.sizeDelta = stars.sizeDelta + new Vector2(24f, 0f);
        }
    }
}
