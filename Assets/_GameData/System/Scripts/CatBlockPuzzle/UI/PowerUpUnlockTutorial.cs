using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The guided power-up introduction, modelled on the ZombieRush unlock tutorial: the screen dims,
    /// the power-up's icon grows in the centre with a bouncing message, flies onto its real HUD
    /// button, and a hand points at that button until the player taps it. Only that button works
    /// meanwhile; the dim blocks the board.
    ///
    /// The overlay is the prefab Resources/CatBlockPuzzle/PowerUpUnlockTutorial, built by
    /// Cat Block Puzzle > Build Power Up Unlock Tutorial Prefab. It carries its own sorting canvas
    /// above the gameplay canvas; the target button is lifted above the dim with a temporary
    /// override canvas and dropped back when the tutorial ends or is cancelled.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PowerUpUnlockTutorial : MonoBehaviour
    {
        public const string PrefabResourcePath = "CatBlockPuzzle/PowerUpUnlockTutorial";
        private const int OverlaySortOffset = 50;
        private const int ButtonSortOffset = 60;
        private const int HandSortOffset = 70;

        [Header("Layers")]
        [SerializeField] private CanvasGroup dim;
        [SerializeField] private RectTransform iconRoot;
        [SerializeField] private CanvasGroup iconGroup;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text message;
        [SerializeField] private RectTransform hand;

        [Header("Timing")]
        [Min(0f)][SerializeField] private float startDelay = 0.2f;
        [Min(0f)][SerializeField] private float dimFadeDuration = 0.35f;
        [Min(0f)][SerializeField] private float iconPopDuration = 0.35f;
        [Min(0f)][SerializeField] private float iconHoldDuration = 1.4f;
        [Min(0f)][SerializeField] private float travelDuration = 0.5f;
        [Min(0f)][SerializeField] private float iconFadeDuration = 0.2f;
        [Min(1f)][SerializeField] private float typewriterCharactersPerSecond = 45f;

        [Header("Icon")]
        [Min(0f)][SerializeField] private float iconStartScale = 0.6f;
        [Min(0f)][SerializeField] private float iconTargetScale = 0.45f;

        [Header("Hand")]
        [Tooltip("Z rotation of the hand while it points at a power-up button (the artwork points up-left).")]
        [SerializeField] private float handRotation = 135f;
        [Tooltip("Offset from the button's centre to the fingertip.")]
        [SerializeField] private Vector2 handOffset = new Vector2(0f, 30f);
        [Tooltip("How far the hand dips towards the button on each tap.")]
        [SerializeField] private float handTapDistance = 22f;

        private Button target;
        private Canvas liftedCanvas;
        private GraphicRaycaster liftedRaycaster;
        private bool addedLiftCanvas;
        private bool addedLiftRaycaster;
        private bool clicked;
        private Coroutine handRoutine;

        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;
        private RectTransform Root => (RectTransform)transform;

        /// <summary>Instantiates the prefab over <paramref name="gameplayCanvas"/>; null when the prefab is missing or broken.</summary>
        public static PowerUpUnlockTutorial Create(Canvas gameplayCanvas, Font hudFont)
        {
            if (gameplayCanvas == null) return null;
            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, gameplayCanvas.transform, false);
            instance.name = "Power Up Unlock Tutorial";
            var tutorial = instance.GetComponent<PowerUpUnlockTutorial>();
            if (tutorial == null || tutorial.dim == null || tutorial.iconRoot == null || tutorial.iconGroup == null ||
                tutorial.iconImage == null || tutorial.message == null || tutorial.hand == null)
            {
                Debug.LogError("PowerUpUnlockTutorial prefab is missing its references; rebuild it from the Cat Block Puzzle menu.", prefab);
                Destroy(instance);
                return null;
            }

            RectTransform root = tutorial.Root;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();
            SortAbove(instance.GetComponent<Canvas>(), gameplayCanvas, OverlaySortOffset);
            SortAbove(tutorial.hand.GetComponent<Canvas>(), gameplayCanvas, HandSortOffset);
            if (hudFont != null && tutorial.message.font == Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")) tutorial.message.font = hudFont;
            tutorial.HideImmediate();
            return tutorial;
        }

        private static void SortAbove(Canvas canvas, Canvas root, int offset)
        {
            if (canvas == null) return;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = root.rootCanvas.sortingLayerID;
            canvas.sortingOrder = root.rootCanvas.sortingOrder + offset;
        }

        /// <summary>
        /// Runs the whole introduction. <paramref name="onArmed"/> runs the moment the icon lands on
        /// the button (unlock it and grant the free charge there); <paramref name="onTapped"/> runs
        /// once the player taps it and the dim has faded.
        /// </summary>
        public IEnumerator Run(Button button, Sprite icon, string text, Action onArmed, Action onTapped)
        {
            target = button;
            bool reduced = ReducedMotion;
            if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

            dim.blocksRaycasts = true;
            yield return Fade(dim, 0f, 1f, reduced ? 0f : dimFadeDuration);

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconRoot.gameObject.SetActive(true);
            iconRoot.anchoredPosition = Vector2.zero;
            StartCoroutine(Typewriter(text));
            yield return PopIcon(reduced ? 0f : iconPopDuration);
            yield return new WaitForSecondsRealtime(iconHoldDuration);

            // The message leaves the moment the icon starts moving, so the two read as one motion.
            StartCoroutine(Fade(MessageGroup, MessageGroup.alpha, 0f, 0.2f));
            Vector2 landing = ButtonPoint();
            yield return FlyIcon(landing, reduced ? 0f : travelDuration);
            yield return Fade(iconGroup, 1f, 0f, reduced ? 0f : iconFadeDuration);
            iconRoot.gameObject.SetActive(false);

            onArmed?.Invoke();
            LiftButton();
            clicked = false;
            if (target != null) target.onClick.AddListener(OnTargetClicked);
            hand.gameObject.SetActive(true);
            handRoutine = StartCoroutine(TapHand(reduced));

            while (!clicked && target != null && target.isActiveAndEnabled) yield return null;

            EndPointing();
            yield return Fade(dim, dim.alpha, 0f, reduced ? 0f : dimFadeDuration);
            dim.blocksRaycasts = false;
            onTapped?.Invoke();
        }

        /// <summary>Puts the button back and hides everything at once; safe to call any number of times.</summary>
        public void Cancel()
        {
            StopAllCoroutines();
            EndPointing();
            HideImmediate();
        }

        private void OnDestroy() => EndPointing();

        private void OnTargetClicked() => clicked = true;

        private void EndPointing()
        {
            if (handRoutine != null) { StopCoroutine(handRoutine); handRoutine = null; }
            if (hand != null) hand.gameObject.SetActive(false);
            if (target != null) target.onClick.RemoveListener(OnTargetClicked);
            DropButton();
        }

        private void HideImmediate()
        {
            dim.alpha = 0f;
            dim.blocksRaycasts = false;
            iconGroup.alpha = 0f;
            iconRoot.gameObject.SetActive(false);
            MessageGroup.alpha = 0f;
            message.text = string.Empty;
            hand.gameObject.SetActive(false);
        }

        private CanvasGroup MessageGroup
        {
            get
            {
                var group = message.GetComponent<CanvasGroup>();
                if (group == null) group = message.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                return group;
            }
        }

        // ------------------------------------------------------------------ button lift

        // The HUD button renders and receives taps above the dim through its own sorting canvas.
        private void LiftButton()
        {
            if (target == null) return;
            Canvas gameplay = Root.parent != null ? Root.parent.GetComponentInParent<Canvas>() : null;
            liftedCanvas = target.GetComponent<Canvas>();
            addedLiftCanvas = liftedCanvas == null;
            if (addedLiftCanvas) liftedCanvas = target.gameObject.AddComponent<Canvas>();
            if (gameplay != null) SortAbove(liftedCanvas, gameplay, ButtonSortOffset);
            liftedRaycaster = target.GetComponent<GraphicRaycaster>();
            addedLiftRaycaster = liftedRaycaster == null;
            if (addedLiftRaycaster) liftedRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
        }

        private void DropButton()
        {
            // Take effect this frame: Destroy only lands at the end of it.
            if (liftedRaycaster != null) liftedRaycaster.enabled = !addedLiftRaycaster && liftedRaycaster.enabled;
            if (liftedCanvas != null) liftedCanvas.overrideSorting = false;
            if (addedLiftRaycaster && liftedRaycaster != null) Destroy(liftedRaycaster);
            if (addedLiftCanvas && liftedCanvas != null) Destroy(liftedCanvas);
            liftedCanvas = null;
            liftedRaycaster = null;
            addedLiftCanvas = addedLiftRaycaster = false;
        }

        // ------------------------------------------------------------------ motion

        private Vector2 ButtonPoint()
        {
            if (target == null) return Vector2.zero;
            Canvas owner = GetComponentInParent<Canvas>();
            Camera cam = owner != null && owner.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? owner.rootCanvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, ((RectTransform)target.transform).position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, cam, out Vector2 local);
            return local;
        }

        private IEnumerator PopIcon(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                iconGroup.alpha = t;
                iconRoot.localScale = Vector3.one * Mathf.LerpUnclamped(iconStartScale, 1f, EaseOutBack(t));
                yield return null;
            }
            iconGroup.alpha = 1f;
            iconRoot.localScale = Vector3.one;
        }

        private IEnumerator FlyIcon(Vector2 destination, float duration)
        {
            Vector2 start = iconRoot.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));
                iconRoot.anchoredPosition = Vector2.LerpUnclamped(start, destination, t);
                iconRoot.localScale = Vector3.one * Mathf.Lerp(1f, iconTargetScale, t);
                yield return null;
            }
            iconRoot.anchoredPosition = destination;
            iconRoot.localScale = Vector3.one * iconTargetScale;
        }

        // Types the message in while the label springs up from below, like the zombie bounce/fade.
        private IEnumerator Typewriter(string text)
        {
            text = text ?? string.Empty;
            CanvasGroup group = MessageGroup;
            RectTransform rect = message.rectTransform;
            Vector2 rest = rect.anchoredPosition;
            float elapsed = 0f;
            float typeDuration = text.Length / typewriterCharactersPerSecond;
            const float bounce = 0.35f;
            while (elapsed < Mathf.Max(typeDuration, bounce))
            {
                elapsed += Time.unscaledDeltaTime;
                int shown = Mathf.Clamp(Mathf.CeilToInt(elapsed * typewriterCharactersPerSecond), 0, text.Length);
                message.text = text.Substring(0, shown);
                float b = Mathf.Clamp01(elapsed / bounce);
                group.alpha = b;
                rect.anchoredPosition = rest + Vector2.down * (1f - EaseOutBack(b)) * 40f;
                yield return null;
            }
            message.text = text;
            group.alpha = 1f;
            rect.anchoredPosition = rest;
        }

        private IEnumerator TapHand(bool reduced)
        {
            hand.localEulerAngles = new Vector3(0f, 0f, handRotation);
            Vector2 rest = ButtonPoint() + handOffset;
            // The finger points along the hand's rotated "up-left" direction.
            float angle = (handRotation + 135f) * Mathf.Deg2Rad;
            Vector2 toward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float cycle = 0f;
            while (true)
            {
                cycle += Time.unscaledDeltaTime;
                float dip = reduced ? 0f : Mathf.Max(0f, Mathf.Sin(cycle * Mathf.PI * 1.6f));
                hand.anchoredPosition = rest - toward * (handTapDistance * (1f - dip));
                hand.localScale = Vector3.one * (1f - 0.06f * dip);
                yield return null;
            }
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = to;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
