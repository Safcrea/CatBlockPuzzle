using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Swaps the whole timer counter (pill and clock icon) to its icy artwork while the Freeze
    /// power-up holds the countdown, with a short pop on freezing and on thawing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimerFreezeSkin : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite frozenBackground;
        [SerializeField] private Sprite frozenIcon;
        [Tooltip("Timer digits while frozen; the normal colours come from the timer itself.")]
        [SerializeField] private Color frozenTextColor = new Color(0.04f, 0.27f, 0.55f, 1f);
        [SerializeField, Min(0f)] private float popDuration = 0.28f;
        [SerializeField, Min(1f)] private float popScale = 1.12f;

        private Sprite normalBackground;
        private Sprite normalIcon;
        private bool captured;
        private bool frozen;
        private Vector3 restingScale = Vector3.one;
        private Coroutine popRoutine;

        public bool IsFrozenLook => frozen;
        public Color FrozenTextColor => frozenTextColor;

        public void SetFrozen(bool value)
        {
            Capture();
            if (value == frozen) return;
            frozen = value;
            if (background != null) background.sprite = frozen && frozenBackground != null ? frozenBackground : normalBackground;
            if (icon != null) icon.sprite = frozen && frozenIcon != null ? frozenIcon : normalIcon;
            if (popRoutine != null) StopCoroutine(popRoutine);
            transform.localScale = restingScale;
            if (isActiveAndEnabled && popDuration > 0f && !MenuTransition.ReducedMotion) popRoutine = StartCoroutine(Pop());
        }

        private void Capture()
        {
            if (captured) return;
            captured = true;
            if (background == null) background = GetComponent<Image>();
            normalBackground = background != null ? background.sprite : null;
            normalIcon = icon != null ? icon.sprite : null;
            restingScale = transform.localScale;
        }

        private IEnumerator Pop()
        {
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                transform.localScale = restingScale * (1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = restingScale;
            popRoutine = null;
        }

        private void OnDisable()
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = null;
            if (captured) transform.localScale = restingScale;
        }
    }
}
