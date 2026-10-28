using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    // Gate input without exposing Selectable's grey disabled artwork during a fade.
    internal sealed class ResultScreenButtons
    {
        private sealed class Entry
        {
            public Button button;
            public CanvasGroup group;
            public Selectable.Transition transition;
        }
        private readonly List<Entry> entries = new List<Entry>();
        public bool Ready { get; private set; }

        public void Prepare(params Button[] buttons)
        {
            Restore();
            entries.Clear();
            foreach (Button button in buttons)
            {
                if (button == null) continue;
                CanvasGroup group = button.GetComponent<CanvasGroup>();
                if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                entries.Add(new Entry { button = button, group = group, transition = button.transition });
            }
            Set(0f, false);
        }

        public void Set(float alpha, bool ready)
        {
            Ready = ready;
            foreach (Entry entry in entries)
            {
                if (entry.button == null || entry.group == null) continue;
                // Turn off visual state changes before gating input; restore normal hover/press on reveal.
                entry.button.transition = Selectable.Transition.None;
                entry.button.interactable = true;
                entry.group.alpha = alpha;
                entry.group.interactable = ready;
                // Keep fading controls as hit surfaces so quick taps cannot reach the board beneath them.
                entry.group.blocksRaycasts = true;
                if (ready) entry.button.transition = entry.transition;
            }
        }

        public IEnumerator Fade(bool visible, float duration = .22f)
        {
            float from = entries.Count > 0 && entries[0].group != null ? entries[0].group.alpha : 0f;
            Set(from, false);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Set(Mathf.Lerp(from, visible ? 1f : 0f, t * t * (3f - 2f * t)), false);
                yield return null;
            }
            Set(visible ? 1f : 0f, visible);
        }

        public void Restore() => Set(1f, true);
    }
}
