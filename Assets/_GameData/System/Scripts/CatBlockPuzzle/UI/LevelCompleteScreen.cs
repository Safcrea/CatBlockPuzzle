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
        [SerializeField] private Button skipButton;
        [Tooltip("Watch a rewarded ad to receive this level's coins a second time.")]
        [SerializeField] private Button doubleCoinsButton;
        [Header("Sequence (auto-bound for the existing win screen)")]
        [SerializeField] private RectTransform titleArtwork;
        [Tooltip("The entire board to reveal after the earned stars.")]
        [SerializeField] private RectTransform coinBoard;
        [SerializeField] private RectTransform coinReward;
        [SerializeField] private Text rewardText;
        [SerializeField, Min(0.1f)] private float popDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float coinCountDuration = 0.9f;
        [SerializeField, Min(0f)] private float starPause = 0.08f;
        [Tooltip("Multiplier for the complete-screen sequence. Lower values play faster.")]
        [SerializeField, Range(0.35f, 1f)] private float sequenceTimeScale = 0.62f;

        private readonly Dictionary<Transform, Vector3> restingScales = new Dictionary<Transform, Vector3>();
        private Image[] winStarOverlays;
        private Coroutine entrance;
        private bool continueWasInteractable;
        private int shownReward;
        private bool doubleCoinsClaimed;

        public const string DoubleCoinsPlacement = "level_complete_2x";
        public bool IsDoubleCoinsOffered => doubleCoinsButton != null && doubleCoinsButton.gameObject.activeSelf && !doubleCoinsClaimed;
        public int ShownReward => shownReward;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() { BindPresentation(); BindButtons(); }
        private void OnEnable() => EnsureBindings();
        private void OnDisable() => CancelEntrance();
        private void Update()
        {
            // The component may live outside the overlay and survive its deactivation.
            if (entrance != null && !IsOpen) CancelEntrance();
        }
        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Next);
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipLevel);
            if (doubleCoinsButton != null) doubleCoinsButton.onClick.RemoveListener(ClaimDoubleCoins);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            CancelEntrance();
            root = screenRoot;
            panel = FindChild(root.transform, "Win Panel") as RectTransform;
            if (panel == null) panel = root.transform as RectTransform;
            titleArtwork = null;
            coinBoard = null;
            coinReward = null;
            rewardText = null;
            if (continueButton != null) continueButton.onClick.RemoveListener(Next);
            continueButton = null;
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipLevel);
            skipButton = null;
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
            if (coinBoard != null) coinBoard.gameObject.SetActive(false);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (continueButton != null) continueWasInteractable = continueButton.interactable;
            shownReward = Mathf.Max(0, reward);
            doubleCoinsClaimed = false;
            // Nothing to double when the level paid nothing (e.g. the testing navigation buttons).
            if (doubleCoinsButton != null)
            {
                doubleCoinsButton.gameObject.SetActive(shownReward > 0);
                doubleCoinsButton.interactable = false;
            }
            entrance = StartCoroutine(PlayEntrance(Mathf.Clamp(starCount, 0, stars.Length), shownReward));
        }

        public void Hide()
        {
            CancelEntrance();
            if (root != null) root.SetActive(false);
        }

        private IEnumerator PlayEntrance(int earnedStars, int reward)
        {
            float timing = Mathf.Clamp(sequenceTimeScale, 0.35f, 1f);
            EnsureWinStarOverlays();
            SetOverlaysVisible(false);
            HideForEntrance(titleArtwork);
            HideForEntrance(coinBoard);
            CacheScale(coinReward);
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
            if (doubleCoinsButton != null) HideForEntrance(doubleCoinsButton.transform);
            SetReward(0);
            countedReward = 0;
            // Ensure the first frame is prepared even if optional bindings are missing.
            yield return null;
            yield return Pop(titleArtwork, popDuration * .8f * timing);
            SoundManager.EnsureInstance().PlaySfxWithMusicDuck("Win");
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                yield return Pop(stars[i].transform, popDuration * .42f * timing);
                if (i < earnedStars && winStarOverlays[i] != null)
                {
                    winStarOverlays[i].gameObject.SetActive(true);
                    yield return Pop(winStarOverlays[i].transform, popDuration * .56f * timing, true);
                }
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, starPause * timing));
            }
            // Reveal the parent board, then wait for its pop to finish before counting.
            if (coinBoard != null) coinBoard.gameObject.SetActive(true);
            if (coinReward != null) coinReward.gameObject.SetActive(true);
            yield return Pop(coinBoard, popDuration * 0.7f * timing);
            float duration = Mathf.Max(0.1f, coinCountDuration * timing);
            float elapsed = 0f;
            while (reward > 0 && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetCountedReward(Mathf.RoundToInt(Mathf.Lerp(0f, reward, 1f - Mathf.Pow(1f - t, 3f))));
                if (coinReward != null)
                    coinReward.localScale = restingScales[coinReward] * (1f + Mathf.Sin(t * Mathf.PI) * 0.04f);
                yield return null;
            }
            SetReward(reward);
            RestoreScale(coinReward);
            yield return new WaitForSecondsRealtime(0.15f * timing);
            if (doubleCoinsButton != null && doubleCoinsButton.gameObject.activeSelf)
            {
                yield return Pop(doubleCoinsButton.transform, popDuration * timing);
                doubleCoinsButton.interactable = true;
            }
            if (continueButton != null)
            {
                yield return Pop(continueButton.transform, popDuration * timing);
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
                float grow = 1f - Mathf.Pow(1f - t, 3f);
                float spring = Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.16f;
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
                if (doubleCoinsButton != null) doubleCoinsButton.interactable = !doubleCoinsClaimed;
            }
            foreach (var entry in restingScales)
                if (entry.Key != null) entry.Key.localScale = entry.Value;
            SetOverlaysVisible(false);
        }
        /// <summary>Shows a counting-up value and ticks the coin-count sound whenever the number changes.</summary>
        private void SetCountedReward(int amount)
        {
            if (amount != countedReward && amount > 0) ButtonSfx.PlayCoinCountTick();
            countedReward = amount;
            SetReward(amount);
        }

        private int countedReward;

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
            // Existing scenes may bind coinReward to Board/Coins rather than Board.
            if (coinBoard == null) coinBoard = FindChild(root.transform, "Board") as RectTransform;
            if (coinBoard == null) coinBoard = coinReward;
            if (rewardText == null && coinReward != null) rewardText = coinReward.GetComponentInChildren<Text>(true);
            if (continueButton == null) continueButton = FindButton(root.transform, "Continue");
            if (continueButton == null) continueButton = FindButton(root.transform, "Next Level");
            if (skipButton == null) skipButton = FindButton(root.transform, "Skip Level");
            if (skipButton == null) skipButton = FindButton(root.transform, "Skip");
            if (doubleCoinsButton == null) doubleCoinsButton = FindButton(root.transform, "Double Coins");
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
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Next);
                if (continueButton.onClick.GetPersistentEventCount() == 0) continueButton.onClick.AddListener(Next);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(SkipLevel);
                if (skipButton.onClick.GetPersistentEventCount() == 0) skipButton.onClick.AddListener(SkipLevel);
            }
            if (doubleCoinsButton != null)
            {
                doubleCoinsButton.onClick.RemoveListener(ClaimDoubleCoins);
                doubleCoinsButton.onClick.AddListener(ClaimDoubleCoins);
            }
        }

        /// <summary>Plays a rewarded ad, then pays the level's coins again and counts the board up to the doubled total.</summary>
        public void ClaimDoubleCoins()
        {
            if (doubleCoinsClaimed || shownReward <= 0 || doubleCoinsButton == null || RewardedAds.IsRequestPending) return;
            ButtonSfx.PlayClick();
            // Lock both choices while the ad is up so Next cannot skip past an unpaid reward.
            doubleCoinsButton.interactable = false;
            bool continueReady = continueButton != null && continueButton.interactable;
            if (continueButton != null) continueButton.interactable = false;
            RewardedAds.Show(DoubleCoinsPlacement,
                onRewarded: () =>
                {
                    doubleCoinsClaimed = true;
                    GameSystem.Instance?.AwardCoins(shownReward);
                    GameAnalytics.CurrencyEarned(shownReward, GameAnalytics.EarnAdDouble, GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0);
                    if (continueButton != null) continueButton.interactable = continueReady;
                    if (IsOpen) StartCoroutine(PlayDoubledReward());
                    else doubleCoinsButton.gameObject.SetActive(false);
                },
                onFailed: outcome =>
                {
                    // No ad or skipped: keep the offer so the player can try again.
                    doubleCoinsButton.interactable = true;
                    if (continueButton != null) continueButton.interactable = continueReady;
                });
        }

        private IEnumerator PlayDoubledReward()
        {
            int from = shownReward, to = shownReward * 2;
            float duration = Mathf.Max(0.1f, coinCountDuration * Mathf.Clamp(sequenceTimeScale, 0.35f, 1f));
            CacheScale(doubleCoinsButton.transform);
            Vector3 buttonScale = restingScales[doubleCoinsButton.transform];
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetCountedReward(Mathf.RoundToInt(Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t, 3f))));
                if (coinReward != null && restingScales.ContainsKey(coinReward))
                    coinReward.localScale = restingScales[coinReward] * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
                doubleCoinsButton.transform.localScale = buttonScale * (1f - t);
                yield return null;
            }
            SetReward(to);
            RestoreScale(coinReward);
            doubleCoinsButton.transform.localScale = buttonScale;
            doubleCoinsButton.gameObject.SetActive(false);
        }
        public void SkipLevel() { ButtonSfx.PlayClick(); GameSystem.Instance?.SkipLevel(); }
        private void Next() { ButtonSfx.PlayClick(); GameSystem.Instance?.NextLevel(); }
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
