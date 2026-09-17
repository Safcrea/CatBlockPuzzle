using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Top-to-bottom win celebration: title, earned stars, coins, Continue.</summary>
    public sealed class LevelCompleteScreen : MonoBehaviour
    {
        [System.Serializable]
        internal sealed class StarSprites
        {
            public Sprite blackAndWhite;
            public Sprite disabled;
            public Sprite winStars;
        }

        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [Header("Shared star artwork")]
        [SerializeField] internal StarSprites sprites = new StarSprites();
        [Header("On-screen image placeholders, ordered left to right")]
        [SerializeField] internal Image[] stars = new Image[3];
        [SerializeField] private Button continueButton;
        [Header("Sequence (auto-bound for the existing win screen)")]
        [SerializeField] private RectTransform titleArtwork;
        [SerializeField] private RectTransform coinReward;
        [SerializeField] private Text rewardText;
        [SerializeField, Min(0.1f)] private float popDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float coinCountDuration = 0.9f;
        [SerializeField, Min(0f)] private float starPause = 0.08f;

        private readonly Dictionary<Transform, Vector3> restingScales = new Dictionary<Transform, Vector3>();
        private Image[] winStarOverlays;
        private Coroutine entrance;
        private bool continueWasInteractable;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() { BindPresentation(); BindButtons(); }
        private void OnEnable() => BindButtons();
        private void OnDisable() => CancelEntrance();
        private void Update()
        {
            // The component may live outside the overlay and survive its deactivation.
            if (entrance != null && !IsOpen) CancelEntrance();
        }
        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Next);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            CancelEntrance();
            root = screenRoot;
            panel = FindChild(root.transform, "Win Panel") as RectTransform;
            if (panel == null) panel = root.transform as RectTransform;
            titleArtwork = null;
            coinReward = null;
            rewardText = null;
            if (continueButton != null) continueButton.onClick.RemoveListener(Next);
            continueButton = null;
            var placeholders = new List<Image>(3);
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("Result Star", System.StringComparison.OrdinalIgnoreCase) ||
                    image.name.StartsWith("Star Placeholder", System.StringComparison.OrdinalIgnoreCase))
                    placeholders.Add(image);
            }
            if (placeholders.Count > 0) stars = placeholders.ToArray();
            EnsureBindings();
        }

        // Gameplay grants the reward; this sequence only presents the amount earned.
        public void Show(string title, int starCount, int reward, int bestStars, int bestCombo)
        {
            if (root == null) return;
            CancelEntrance();
            EnsureBindings();
            if (coinReward != null) coinReward.gameObject.SetActive(false);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (continueButton != null) continueWasInteractable = continueButton.interactable;
            entrance = StartCoroutine(PlayEntrance(Mathf.Clamp(starCount, 0, stars.Length), Mathf.Max(0, reward)));
        }

        public void Hide()
        {
            CancelEntrance();
            if (root != null) root.SetActive(false);
        }

        private IEnumerator PlayEntrance(int earnedStars, int reward)
        {
            EnsureWinStarOverlays();
            SetOverlaysVisible(false);
            HideForEntrance(titleArtwork);
            HideForEntrance(coinReward);
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                Sprite disabledSprite = sprites != null ? sprites.disabled : null;
                if (disabledSprite == null && sprites != null) disabledSprite = sprites.blackAndWhite;
                if (disabledSprite != null) stars[i].sprite = disabledSprite;
                stars[i].gameObject.SetActive(true);
                HideForEntrance(stars[i].transform);
            }
            if (continueButton != null)
            {
                continueButton.interactable = false;
                HideForEntrance(continueButton.transform);
            }
            SetReward(0);
            // Ensure the first frame is prepared even if optional bindings are missing.
            yield return null;
            yield return Pop(titleArtwork, popDuration);
            for (int i = 0; i < stars.Length; i++)
                if (stars[i] != null) RestoreScale(stars[i].transform);
            for (int i = 0; i < earnedStars; i++)
            {
                if (winStarOverlays[i] == null) continue;
                winStarOverlays[i].gameObject.SetActive(true);
                yield return Pop(winStarOverlays[i].transform, popDuration, true);
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, starPause));
            }
            if (coinReward != null) coinReward.gameObject.SetActive(true);
            yield return Pop(coinReward, popDuration * 0.7f);
            float duration = Mathf.Max(0.1f, coinCountDuration);
            float elapsed = 0f;
            while (reward > 0 && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetReward(Mathf.RoundToInt(Mathf.Lerp(0f, reward, 1f - Mathf.Pow(1f - t, 3f))));
                if (coinReward != null)
                    coinReward.localScale = restingScales[coinReward] * (1f + Mathf.Sin(t * Mathf.PI * 8f) * 0.045f);
                yield return null;
            }
            SetReward(reward);
            RestoreScale(coinReward);
            yield return new WaitForSecondsRealtime(0.15f);
            if (continueButton != null)
            {
                yield return Pop(continueButton.transform, popDuration);
                continueButton.interactable = continueWasInteractable;
            }
            entrance = null;
        }

        private IEnumerator Pop(Transform target, float duration, bool jelly = false)
        {
            if (target == null) yield break;
            CacheScale(target);
            Vector3 scale = restingScales[target];
            duration = Mathf.Max(0.1f, duration);
            float elapsed = 0f;
            target.localScale = Vector3.zero;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.35f), 3f);
                float spring = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 0.28f;
                target.localScale = Vector3.Scale(scale, jelly
                    ? new Vector3(grow * (1f + spring), grow * (1f - spring * 0.75f), 1f)
                    : Vector3.one * (grow * (1f + spring)));
                yield return null;
            }
            target.localScale = scale;
        }

        private void CacheScale(Transform target)
        {
            if (target != null && !restingScales.ContainsKey(target)) restingScales.Add(target, target.localScale);
        }
        private void HideForEntrance(Transform target)
        {
            if (target == null) return;
            CacheScale(target);
            target.localScale = Vector3.zero;
        }
        private void RestoreScale(Transform target)
        {
            if (target != null && restingScales.TryGetValue(target, out Vector3 scale)) target.localScale = scale;
        }
        private void CancelEntrance()
        {
            if (entrance != null)
            {
                StopCoroutine(entrance);
                entrance = null;
                if (continueButton != null) continueButton.interactable = continueWasInteractable;
            }
            foreach (var entry in restingScales)
                if (entry.Key != null) entry.Key.localScale = entry.Value;
            SetOverlaysVisible(false);
        }
        private void SetReward(int amount)
        {
            if (rewardText != null) rewardText.text = "+" + amount + " coins";
        }

        private void BindPresentation()
        {
            if (root == null) return;
            if (stars == null) stars = new Image[0];
            if (titleArtwork == null) titleArtwork = FindChild(root.transform, "Level Complete Artwork") as RectTransform;
            if (titleArtwork == null) titleArtwork = FindChild(root.transform, "Title") as RectTransform;
            if (coinReward == null) coinReward = FindChild(root.transform, "Coin Reward Artwork") as RectTransform;
            if (coinReward == null) coinReward = FindChild(root.transform, "Board") as RectTransform;
            if (rewardText == null && coinReward != null) rewardText = coinReward.GetComponentInChildren<Text>(true);
            if (continueButton == null) continueButton = FindButton(root.transform, "Continue");
            if (continueButton == null) continueButton = FindButton(root.transform, "Next Level");
        }

        private void EnsureWinStarOverlays()
        {
            if (winStarOverlays == null || winStarOverlays.Length != stars.Length) winStarOverlays = new Image[stars.Length];
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                // Child overlays inherit layout/rotation/scale and leave disabled slots intact.
                Transform existing = stars[i].transform.Find("Win Star Overlay");
                Image overlay = existing != null ? existing.GetComponent<Image>() : null;
                if (overlay == null)
                {
                    var overlayObject = new GameObject("Win Star Overlay", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    overlayObject.transform.SetParent(stars[i].transform, false);
                    overlayObject.GetComponent<LayoutElement>().ignoreLayout = true;
                    overlay = overlayObject.GetComponent<Image>();
                    RectTransform rect = overlay.rectTransform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = stars[i].rectTransform.pivot;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                }
                overlay.sprite = sprites != null ? sprites.winStars : null;
                overlay.preserveAspect = stars[i].preserveAspect;
                overlay.type = stars[i].type;
                overlay.raycastTarget = false;
                overlay.enabled = overlay.sprite != null;
                winStarOverlays[i] = overlay;
            }
        }
        private void SetOverlaysVisible(bool visible)
        {
            if (winStarOverlays == null) return;
            foreach (Image overlay in winStarOverlays)
                if (overlay != null) overlay.gameObject.SetActive(visible);
        }
        private void BindButtons()
        {
            if (continueButton == null) return;
            continueButton.onClick.RemoveListener(Next);
            if (continueButton.onClick.GetPersistentEventCount() == 0) continueButton.onClick.AddListener(Next);
        }
        private void Next() => GameSystem.Instance?.NextLevel();
        private static Button FindButton(Transform parent, string name)
        {
            Transform found = FindChild(parent, name);
            return found != null ? found.GetComponent<Button>() : null;
        }
        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
