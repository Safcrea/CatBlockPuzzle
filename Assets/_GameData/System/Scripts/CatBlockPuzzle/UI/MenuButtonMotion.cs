using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Idle motion for a menu button, one of several styles so neighbouring buttons feel distinct,
    /// plus a squash while pressed and a small spring on release. Runs on real time so it keeps
    /// moving while gameplay is paused, and restores the authored transform when disabled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Style
        {
            /// <summary>Slow continuous rock from side to side.</summary>
            Sway,
            /// <summary>A little jump with a squash on landing, once per interval.</summary>
            Hop,
            /// <summary>A quick decaying shake, once per interval.</summary>
            Wiggle,
            /// <summary>Barely-there continuous scale breathing.</summary>
            Breathe,
            /// <summary>One rotational twist and back, once per interval.</summary>
            Twist,
        }

        [Serializable]
        public sealed class Settings
        {
            public Style style = Style.Sway;
            [Tooltip("Degrees for rotating styles, pixels for Hop, scale fraction for Breathe.")]
            public float amount = 2.5f;
            [Min(0.2f)] public float interval = 3f;
            [Min(0.1f)] public float duration = 0.6f;
            [Min(0f)] public float startDelay;
            [Range(0.8f, 1f)] public float pressedScale = 0.93f;
            [Range(0f, 0.2f)] public float releaseOvershoot = 0.05f;

            public static Settings For(Style style, float startDelay = 0f)
            {
                var settings = new Settings { style = style, startDelay = startDelay };
                switch (style)
                {
                    case Style.Sway: settings.amount = 2.5f; settings.interval = 3.2f; break;
                    case Style.Hop: settings.amount = 9f; settings.interval = 3.6f; settings.duration = 0.55f; break;
                    case Style.Wiggle: settings.amount = 7f; settings.interval = 4f; settings.duration = 0.7f; break;
                    case Style.Breathe: settings.amount = 0.02f; settings.interval = 2.6f; break;
                    case Style.Twist: settings.amount = 8f; settings.interval = 4.4f; settings.duration = 0.8f; break;
                }
                return settings;
            }
        }

        private Settings settings = new Settings();
        private RectTransform rect;
        private Vector3 restingScale = Vector3.one;
        private Vector2 restingPosition;
        private Quaternion restingRotation = Quaternion.identity;
        private float elapsed;
        private float pressBlend;
        private float releaseElapsed = -1f;
        private bool pressed;

        public Settings Current => settings;

        public static MenuButtonMotion Configure(Transform target, Style style, float startDelay = 0f)
            => Configure(target, Settings.For(style, startDelay));

        public static MenuButtonMotion Configure(Transform target, Settings settings)
        {
            if (target == null) return null;
            var motion = target.GetComponent<MenuButtonMotion>();
            if (motion == null) motion = target.gameObject.AddComponent<MenuButtonMotion>();
            motion.settings = settings ?? new Settings();
            motion.enabled = true;
            return motion;
        }

        private void Awake()
        {
            rect = transform as RectTransform;
            CaptureResting();
        }

        private void CaptureResting()
        {
            restingScale = transform.localScale;
            restingRotation = transform.localRotation;
            if (rect != null) restingPosition = rect.anchoredPosition;
        }

        private void OnEnable()
        {
            elapsed = 0f;
            pressBlend = 0f;
            releaseElapsed = -1f;
            pressed = false;
        }

        private void OnDisable()
        {
            pressed = false;
            transform.localScale = restingScale;
            transform.localRotation = restingRotation;
            if (rect != null) rect.anchoredPosition = restingPosition;
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>Steps the motion by a slice of real time. Public so tests can drive it without waiting.</summary>
        public void Advance(float deltaSeconds)
        {
            elapsed += deltaSeconds;
            pressBlend = Mathf.MoveTowards(pressBlend, pressed ? 1f : 0f, deltaSeconds * 14f);
            if (releaseElapsed >= 0f) releaseElapsed += deltaSeconds;

            float scale = 1f;
            float angle = 0f;
            Vector2 offset = Vector2.zero;
            float phase = Mathf.Max(0f, elapsed - settings.startDelay);
            float cycle = Mathf.Max(settings.interval, settings.duration + 0.1f);
            float burst = Burst(phase % cycle, settings.duration);

            switch (settings.style)
            {
                case Style.Sway:
                    angle = settings.amount * Mathf.Sin(phase * Mathf.PI * 2f / settings.interval);
                    break;
                case Style.Hop:
                    offset.y = settings.amount * Mathf.Sin(burst * Mathf.PI);
                    // Stretch on the way up, squash as it lands.
                    scale += 0.04f * Mathf.Sin(burst * Mathf.PI * 2f) * (burst > 0f && burst < 1f ? 1f : 0f);
                    break;
                case Style.Wiggle:
                    if (burst > 0f && burst < 1f) angle = settings.amount * Mathf.Sin(burst * Mathf.PI * 5f) * (1f - burst);
                    break;
                case Style.Breathe:
                    scale += settings.amount * (0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f / settings.interval));
                    break;
                case Style.Twist:
                    if (burst > 0f && burst < 1f) angle = settings.amount * Mathf.Sin(burst * Mathf.PI * 2f) * Mathf.Sin(burst * Mathf.PI);
                    break;
            }

            // Pressing squashes the whole button; letting go springs it slightly past rest and back.
            scale = Mathf.Lerp(scale, settings.pressedScale, pressBlend);
            if (releaseElapsed >= 0f)
            {
                const float springSeconds = 0.32f;
                float t = releaseElapsed / springSeconds;
                if (t >= 1f) releaseElapsed = -1f;
                else scale += settings.releaseOvershoot * Mathf.Sin(t * Mathf.PI) * (1f - t);
            }

            transform.localScale = restingScale * scale;
            transform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, angle);
            if (rect != null) rect.anchoredPosition = restingPosition + offset;
        }

        /// <summary>0 outside the burst window at the start of each cycle, 0..1 inside it.</summary>
        private static float Burst(float phaseInCycle, float duration)
        {
            if (duration <= 0f) return 0f;
            return Mathf.Clamp01(phaseInCycle / duration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            releaseElapsed = -1f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressed) releaseElapsed = 0f;
            pressed = false;
        }

        public void OnPointerExit(PointerEventData eventData) => pressed = false;
    }
}
