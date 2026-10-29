using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The sad-cat "Level Failed" popup. The cat, title, caption and buttons are baked art; this component
    /// binds the buttons and plays the entrance: the dimmer fades, the panel drops in with a bounce, the title and
    /// three brown stars wobble in one by one, the caption fades up and the buttons slide in. It then idles
    /// with a slow sigh.
    /// </summary>
    public sealed class LevelFailScreen : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button skipButton;
        [Header("Artwork (auto-bound by name when empty)")]
        [SerializeField] private RectTransform titleArtwork;
        [SerializeField] private RectTransform captionArtwork;
        [SerializeField] private RectTransform[] stars = new RectTransform[0];

        private readonly Dictionary<RectTransform, Vector3> restingScales = new Dictionary<RectTransform, Vector3>();
        private readonly Dictionary<RectTransform, Vector2> restingPositions = new Dictionary<RectTransform, Vector2>();
        private readonly ResultScreenButtons buttons = new ResultScreenButtons();
        private Coroutine presentation;
        private Coroutine action;
        private bool actionPending;
        private bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings()
        {
            if (root == null) return;
            if (panel == null) panel = FindChild(root.transform, "Fail Panel") as RectTransform;
            if (panel == null) panel = root.transform as RectTransform;
            if (titleArtwork == null) titleArtwork = FindChild(root.transform, "Title") as RectTransform;
            if (captionArtwork == null) captionArtwork = FindChild(root.transform, "Caption") as RectTransform;
            if (stars == null || stars.Length == 0 || stars[0] == null)
            {
                var found = new List<RectTransform>(3);
                for (int i = 1; i <= 3; i++)
                {
                    RectTransform star = FindChild(root.transform, "Fail Star " + i) as RectTransform;
                    if (star != null) found.Add(star);
                }
                stars = found.ToArray();
            }
            if (retryButton == null) retryButton = FindButton("Retry");
            if (homeButton == null) homeButton = FindButton("Home");
            if (skipButton == null) skipButton = FindButton("Skip Level");
            if (skipButton == null) skipButton = FindButton("Skip");
            BindButtons();
        }

        private void OnEnable() => EnsureBindings();
        private void OnDisable() => CancelPresentation();
        private void Update()
        {
            // The component may live outside the overlay and survive its deactivation.
            if (presentation != null && !IsOpen) CancelPresentation();
        }
        private void OnDestroy()
        {
            CancelPresentation();
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            CancelPresentation();
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            root = screenRoot;
            panel = null; titleArtwork = null; captionArtwork = null; stars = new RectTransform[0];
            retryButton = null; homeButton = null; skipButton = null;
            restingScales.Clear();
            restingPositions.Clear();
            EnsureBindings();
        }

        // The title and encouragement are baked into the artwork, so the message text is not drawn.
        public void Show(string title, string message)
        {
            if (root == null) return;
            CancelPresentation();
            EnsureBindings();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            buttons.Prepare(retryButton, homeButton, skipButton);
            presentation = StartCoroutine(PlayPresentation());
        }

        public void Hide()
        {
            CancelPresentation();
            if (root != null) root.SetActive(false);
        }

        private RectTransform[] ButtonRects()
        {
            var list = new List<RectTransform>(3);
            foreach (Button button in new[] { retryButton, homeButton, skipButton })
                if (button != null) list.Add((RectTransform)button.transform);
            return list.ToArray();
        }

        private IEnumerator PlayPresentation()
        {
            CanvasGroup dim = Dimmer();
            RectTransform[] buttonRects = ButtonRects();
            Remember(panel); Remember(titleArtwork); Remember(captionArtwork);
            foreach (RectTransform star in stars) Remember(star);
            foreach (RectTransform rect in buttonRects) Remember(rect);
            if (ReducedMotion)
            {
                if (dim != null) dim.alpha = 1f;
                buttons.Set(1f, true);
                presentation = null;
                yield break;
            }

            Hide0(panel); Hide0(titleArtwork); Hide0(captionArtwork);
            foreach (RectTransform star in stars) Hide0(star);
            foreach (RectTransform rect in buttonRects) Hide0(rect);
            const float panelEnd = .55f, titleStart = .30f, starStart = .62f, starGap = .2f;
            float captionStart = starStart + stars.Length * starGap + .05f;
            float buttonStart = captionStart + .18f, buttonGap = .12f;
            float end = buttonStart + buttonGap * Mathf.Max(0, buttonRects.Length - 1) + .5f;
            float elapsed = 0f;
            bool thud = false;
            while (elapsed < end)
            {
                elapsed += Time.unscaledDeltaTime;
                if (dim != null) dim.alpha = Mathf.Clamp01(elapsed / .3f);
                // Panel falls in from above and settles with a squash.
                float p = Mathf.Clamp01(elapsed / panelEnd);
                float drop = EaseOutBounce(p);
                SetScale(panel, Vector3.one * Mathf.Lerp(.82f, 1f, EaseOut(p)));
                SetOffset(panel, new Vector2(0f, (1f - drop) * 260f));
                if (!thud && p >= .7f) { thud = true; HapticsController.Current?.Play(HapticKind.UiTap); }
                Pop(titleArtwork, elapsed - titleStart, .45f, .2f);
                for (int i = 0; i < stars.Length; i++)
                {
                    // Each brown star drops in, wobbles and settles slightly drooped.
                    float t = elapsed - (starStart + i * starGap);
                    Pop(stars[i], t, .5f, .24f);
                    if (stars[i] != null && t > 0f)
                        stars[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Mathf.Clamp01(t / .6f) * Mathf.PI * 2.5f) * (1f - Mathf.Clamp01(t / .6f)) * 14f);
                }
                Pop(captionArtwork, elapsed - captionStart, .4f, .08f);
                for (int i = 0; i < buttonRects.Length; i++) Pop(buttonRects[i], elapsed - (buttonStart + i * buttonGap), .4f, .12f);
                float bt = Mathf.Clamp01((elapsed - buttonStart) / .3f);
                buttons.Set(bt * bt * (3f - 2f * bt), elapsed >= buttonStart + buttonGap * Mathf.Max(0, buttonRects.Length - 1) + .25f);
                yield return null;
            }
            RestoreAll();
            buttons.Set(1f, true);
            // Idle: the whole card sighs slowly and the stars sway.
            float time = 0f;
            while (IsOpen)
            {
                time += Time.unscaledDeltaTime;
                float sigh = Mathf.Sin(time * Mathf.PI * 2f / 4.2f);
                SetScale(panel, Vector3.one * (1f + sigh * .008f));
                for (int i = 0; i < stars.Length; i++)
                    if (stars[i] != null) stars[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.6f + i * 1.3f) * 3f);
                yield return null;
            }
            presentation = null;
        }

        private CanvasGroup Dimmer()
        {
            Transform dimmer = FindChild(root.transform, "Dimmer");
            if (dimmer == null) return null;
            CanvasGroup group = dimmer.GetComponent<CanvasGroup>();
            if (group == null) group = dimmer.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        // ------------------------------------------------------------------ motion helpers

        private void Remember(RectTransform target)
        {
            if (target == null || restingScales.ContainsKey(target)) return;
            restingScales[target] = target.localScale;
            restingPositions[target] = target.anchoredPosition;
        }
        private void Hide0(RectTransform target) { if (target != null) target.localScale = Vector3.zero; }
        private void SetScale(RectTransform target, Vector3 factor)
        {
            if (target == null || !restingScales.TryGetValue(target, out Vector3 rest)) return;
            target.localScale = Vector3.Scale(rest, factor);
        }
        private void SetOffset(RectTransform target, Vector2 offset)
        {
            if (target == null || !restingPositions.TryGetValue(target, out Vector2 rest)) return;
            target.anchoredPosition = rest + offset;
        }
        private void Pop(RectTransform target, float elapsed, float duration, float spring)
        {
            if (target == null || !restingScales.ContainsKey(target)) return;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.05f, duration));
            float grow = 1f - Mathf.Pow(1f - t, 3f);
            float wobble = Mathf.Sin(t * Mathf.PI) * (1f - t) * spring;
            SetScale(target, Vector3.one * (grow * (1f + wobble)));
        }
        private void RestoreAll()
        {
            foreach (var entry in restingScales) if (entry.Key != null) entry.Key.localScale = entry.Value;
            foreach (var entry in restingPositions) if (entry.Key != null) entry.Key.anchoredPosition = entry.Value;
            foreach (RectTransform star in stars) if (star != null) star.localRotation = Quaternion.identity;
        }
        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseOutBounce(float t)
        {
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) { t -= 1.5f / d; return n * t * t + .75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + .9375f; }
            t -= 2.625f / d; return n * t * t + .984375f;
        }

        private void CancelPresentation()
        {
            if (presentation != null) StopCoroutine(presentation);
            presentation = null;
            if (action != null) StopCoroutine(action);
            action = null;
            actionPending = false;
            buttons.Restore();
            RestoreAll();
            if (root != null)
            {
                Transform dimmer = FindChild(root.transform, "Dimmer");
                CanvasGroup group = dimmer != null ? dimmer.GetComponent<CanvasGroup>() : null;
                if (group != null) group.alpha = 1f;
            }
        }

        // ------------------------------------------------------------------ buttons

        private Button FindButton(string buttonName)
        {
            Transform found = FindChild(root.transform, buttonName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private void BindButtons()
        {
            BindIfEmpty(retryButton, Retry);
            BindIfEmpty(homeButton, Home);
            BindIfEmpty(skipButton, Skip);
        }

        private static void BindIfEmpty(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
        }

        private void BeginAction(System.Action callback)
        {
            if (actionPending || !buttons.Ready || !IsOpen) return;
            actionPending = true;
            ButtonSfx.PlayClick();
            if (presentation != null) StopCoroutine(presentation);
            presentation = null;
            RestoreAll();
            action = StartCoroutine(FadeThenAct(callback));
        }

        private IEnumerator FadeThenAct(System.Action callback)
        {
            yield return buttons.Fade(false);
            callback();
        }

        private void Retry() => BeginAction(() =>
        {
            GameAnalytics.SetLevelStartSource(GameAnalytics.SourceRetry);
            GameSystem.Instance?.RestartLevel();
        });
        private void Home() => BeginAction(() => GameSystem.Instance?.GoHome());
        public void SkipLevel() => BeginAction(() => GameSystem.Instance?.SkipLevel());
        private void Skip() => SkipLevel();

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
