using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatBlockPuzzle
{
    /// <summary>Two soft attention pops, then a rest. Uses real time even when gameplay is paused.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuAttentionPulse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 restingScale;
        [Serializable]
        public sealed class Settings
        {
            public bool enabled = true;
            [Range(0f, 0.3f)] public float scaleAmount = 0.065f;
            [Min(1f)] public float interval = 2.8f;
            [Min(0.1f)] public float popDuration = 0.48f;
            [Min(0f)] public float startDelay = 0.15f;
            [Range(0f, 1f)] public float secondPopStrength = 0.45f;
            [Range(0.8f, 1f)] public float pressedScale = 0.94f;
        }

        private Settings settings = new Settings();
        private float elapsed;
        private bool pressed;

        public static void Configure(Transform target, float amount, float period)
            => Configure(target, new Settings { scaleAmount = amount, interval = period });

        public static void Configure(Transform target, Settings settings)
        {
            if (target == null) return;
            var pulse = target.GetComponent<MenuAttentionPulse>();
            if (pulse == null) pulse = target.gameObject.AddComponent<MenuAttentionPulse>();
            pulse.settings = settings ?? new Settings();
            pulse.enabled = true;
        }

        private void Awake() => restingScale = transform.localScale;
        private void OnEnable() { elapsed = 0f; pressed = false; }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.1f, settings.popDuration);
            float start = Mathf.Max(0f, settings.startDelay);
            float secondStart = start + duration + 0.06f;
            float cycle = Mathf.Max(settings.interval, secondStart + duration * 0.67f + 0.2f);
            float phase = elapsed % cycle;
            float pop = settings.enabled
                ? Bump(phase, start, duration) + settings.secondPopStrength * Bump(phase, secondStart, duration * 0.67f)
                : 0f;
            transform.localScale = restingScale * (pressed ? settings.pressedScale : 1f + settings.scaleAmount * pop);
        }

        private static float Bump(float phase, float start, float duration)
        {
            float t = (phase - start) / duration;
            if (t <= 0f || t >= 1f) return 0f;
            float wave = Mathf.Sin(t * Mathf.PI);
            return wave * wave;
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) { pressed = false; elapsed = 0f; }
        public void OnPointerExit(PointerEventData eventData) => pressed = false;
        private void OnDisable() { pressed = false; transform.localScale = restingScale; }
    }
}
