using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>A sad cat reaction followed by an encouraging retry, using the reference artwork.</summary>
    public sealed class LevelFailScreen : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button skipButton;
        [Header("Sad presentation (auto-bound to the existing artwork)")]
        [SerializeField] private RectTransform titleArtwork;
        [SerializeField] private RectTransform sadCatArtwork;

        private struct RestingPose
        {
            public Vector3 scale;
            public Vector2 position;
            public Quaternion rotation;
        }
        private readonly Dictionary<RectTransform, RestingPose> restingPoses = new Dictionary<RectTransform, RestingPose>();
        private Coroutine presentation;
        private Image[] tears;
        private static Sprite tearSprite;
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
            if (sadCatArtwork == null) sadCatArtwork = FindChild(root.transform, "Cat") as RectTransform;
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
            // Some scenes keep this component alive outside the hidden panel.
            if (presentation != null && (!IsOpen || ReducedMotion)) CancelPresentation();
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
            if (tears != null)
                foreach (Image tear in tears) if (tear != null) Destroy(tear.gameObject);
            tears = null;
            restingPoses.Clear();
            root = screenRoot;
            titleArtwork = null;
            sadCatArtwork = null;
            panel = FindChild(root.transform, "Fail Panel") as RectTransform;
            retryButton = FindButton("Retry");
            homeButton = FindButton("Home");
            skipButton = FindButton("Skip Level");
            if (skipButton == null) skipButton = FindButton("Skip");
            EnsureBindings();
        }

        // The title and encouragement are baked into the reference artwork.
        public void Show(string title, string message)
        {
            if (root == null) return;
            CancelPresentation();
            EnsureBindings();
            RememberPose(panel);
            RememberPose(titleArtwork);
            RememberPose(sadCatArtwork);
            RememberPose(retryButton != null ? retryButton.transform as RectTransform : null);
            RememberPose(homeButton != null ? homeButton.transform as RectTransform : null);
            RememberPose(skipButton != null ? skipButton.transform as RectTransform : null);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (!ReducedMotion) presentation = StartCoroutine(PlayPresentation());
        }

        public void Hide()
        {
            CancelPresentation();
            if (root != null) root.SetActive(false);
        }

        private IEnumerator PlayPresentation()
        {
            EnsureTears();
            float elapsed = 0f;
            const float duration = 1.05f;
            while (elapsed < duration && IsOpen)
            {
                float panelT = Mathf.Clamp01(elapsed / .45f);
                AnimatePose(panel, Vector2.one * Mathf.Lerp(.96f, 1f, EaseOut(panelT)),
                    new Vector2(0f, 24f * (1f - EaseOut(panelT))), -1.8f * Mathf.Sin(panelT * Mathf.PI * 2f) * (1f - panelT));
                float titleT = Mathf.Clamp01(elapsed / .48f);
                AnimatePose(titleArtwork, Vector2.one * Mathf.Lerp(.92f, 1f, EaseOut(titleT)),
                    new Vector2(0f, 12f * (1f - EaseOut(titleT))), -3f * Mathf.Sin(titleT * Mathf.PI) * (1f - titleT));
                float catT = Mathf.Clamp01((elapsed - .12f) / .65f);
                float slump = Mathf.Sin(catT * Mathf.PI) * .09f;
                AnimatePose(sadCatArtwork, new Vector2(1f + slump, 1f - slump),
                    new Vector2(0f, 35f * (1f - EaseOut(catT)) - 10f * Mathf.Sin(catT * Mathf.PI)),
                    3f * Mathf.Sin(catT * Mathf.PI * 2f) * (1f - catT));
                AnimateButton(retryButton, elapsed, .35f);
                AnimateButton(homeButton, elapsed, .45f);
                AnimateButton(skipButton, elapsed, .55f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            RestorePoses();
            elapsed = 0f;
            while (IsOpen)
            {
                // A slow sigh and a pair of tears, with a quiet rest between reactions.
                float sigh = Mathf.Sin(elapsed * Mathf.PI * 2f / 4.2f);
                AnimatePose(sadCatArtwork, new Vector2(1f + sigh * .014f, 1f - sigh * .024f),
                    new Vector2(0f, -3f * sigh * sigh), -.9f * sigh);
                AnimateTear(0, elapsed, .4f);
                AnimateTear(1, elapsed, .62f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            RestorePoses();
            HideTears();
            presentation = null;
        }

        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private void AnimateButton(Button button, float elapsed, float delay)
        {
            if (button == null) return;
            float t = Mathf.Clamp01((elapsed - delay) / .35f);
            float scale = Mathf.Lerp(.88f, 1f, EaseOut(t)) + Mathf.Sin(t * Mathf.PI) * .07f;
            AnimatePose(button.transform as RectTransform, Vector2.one * scale, Vector2.zero, 0f);
        }

        private void RememberPose(RectTransform target)
        {
            if (target == null || restingPoses.ContainsKey(target)) return;
            restingPoses.Add(target, new RestingPose { scale = target.localScale, position = target.anchoredPosition, rotation = target.localRotation });
        }

        private void AnimatePose(RectTransform target, Vector2 scale, Vector2 offset, float rotation)
        {
            if (target == null || !restingPoses.TryGetValue(target, out RestingPose rest)) return;
            target.localScale = Vector3.Scale(rest.scale, new Vector3(scale.x, scale.y, 1f));
            target.anchoredPosition = rest.position + offset;
            target.localRotation = rest.rotation * Quaternion.Euler(0f, 0f, rotation);
        }

        private void RestorePoses()
        {
            foreach (var entry in restingPoses)
            {
                if (entry.Key == null) continue;
                entry.Key.localScale = entry.Value.scale;
                entry.Key.anchoredPosition = entry.Value.position;
                entry.Key.localRotation = entry.Value.rotation;
            }
        }

        private void CancelPresentation()
        {
            if (presentation != null) StopCoroutine(presentation);
            presentation = null;
            RestorePoses();
            HideTears();
        }

        private void EnsureTears()
        {
            if (sadCatArtwork == null) return;
            if (tears == null) tears = new Image[2];
            if (tearSprite == null) tearSprite = CreateTearSprite();
            for (int i = 0; i < tears.Length; i++)
            {
                if (tears[i] != null) continue;
                var go = new GameObject("Sad Tear " + (i + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(sadCatArtwork, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                var tear = go.GetComponent<Image>();
                tear.sprite = tearSprite;
                tear.raycastTarget = false;
                tear.rectTransform.anchorMin = tear.rectTransform.anchorMax = new Vector2(.5f, .5f);
                tear.rectTransform.pivot = new Vector2(.5f, .5f);
                go.SetActive(false);
                tears[i] = tear;
            }
        }

        private void AnimateTear(int index, float elapsed, float delay)
        {
            if (tears == null || tears[index] == null || sadCatArtwork == null) return;
            Image tear = tears[index];
            float t = ((elapsed % 3.6f) - delay) / .85f;
            bool visible = t > 0f && t < 1f;
            tear.gameObject.SetActive(visible);
            if (!visible) return;
            Vector2 size = sadCatArtwork.rect.size;
            Image cat = sadCatArtwork.GetComponent<Image>();
            if (cat != null && cat.preserveAspect && cat.sprite != null)
            {
                Vector2 spriteSize = cat.sprite.rect.size;
                size = spriteSize * Mathf.Min(size.x / spriteSize.x, size.y / spriteSize.y);
            }
            float width = size.x * .038f;
            tear.rectTransform.sizeDelta = new Vector2(width, width * 1.5f);
            tear.rectTransform.anchoredPosition = new Vector2((index == 0 ? -.235f : .235f) * size.x,
                (-.12f - .24f * t * t) * size.y);
            tear.rectTransform.localScale = Vector3.one * Mathf.Lerp(.65f, 1f, Mathf.Clamp01(t * 4f));
            tear.color = new Color(.38f, .76f, 1f, Mathf.Sin(t * Mathf.PI) * .9f);
        }

        private void HideTears()
        {
            if (tears == null) return;
            foreach (Image tear in tears) if (tear != null) tear.gameObject.SetActive(false);
        }

        private static Sprite CreateTearSprite()
        {
            var texture = new Texture2D(32, 48, TextureFormat.RGBA32, false) { name = "Sad Tear", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    Vector2 point = new Vector2(x + .5f, y + .5f);
                    float circle = Mathf.Clamp01(12f - Vector2.Distance(point, new Vector2(16f, 15f)));
                    float triangle = y >= 15 ? Mathf.Clamp01((45f - point.y) * .4f - Mathf.Abs(point.x - 16f)) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(circle, triangle)));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 48f), new Vector2(.5f, .5f), 100f);
            sprite.name = "Sad Tear";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

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

        private void Retry() => GameSystem.Instance?.RestartLevel();
        private void Home() => GameSystem.Instance?.GoHome();
        public void SkipLevel() => GameSystem.Instance?.SkipLevel();
        private void Skip() => SkipLevel();

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
