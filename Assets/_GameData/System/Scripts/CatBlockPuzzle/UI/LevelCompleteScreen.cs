using AVNEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Overlapping win celebration with fading result controls.</summary>
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
        [Header("Level Complete art layout")]
        [SerializeField] private Button homeButton;
        [Tooltip("The baked caption line under the coin bar.")]
        [SerializeField] private RectTransform captionArtwork;
        [Tooltip("Applied to the stars the player did not earn (Cat Block Puzzle/UI Grayscale).")]
        [SerializeField] private Material greyStarMaterial;
        [Header("Sequence (auto-bound for the existing win screen)")]
        [SerializeField] private RectTransform titleArtwork;
        [Tooltip("The coin board, revealed alongside the stars and buttons.")]
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
        private readonly ResultScreenButtons buttons = new ResultScreenButtons();
        private Coroutine action;
        private bool actionPending;
        private int presentationVersion;
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
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
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
            presentationVersion++;
            shownReward = Mathf.Max(0, reward);
            doubleCoinsClaimed = false;
            // Nothing to double when the level paid nothing (e.g. the testing navigation buttons).
            if (doubleCoinsButton != null)
            {
                doubleCoinsButton.gameObject.SetActive(shownReward > 0);
            }
            buttons.Prepare(continueButton, skipButton, doubleCoinsButton);
            entrance = StartCoroutine(PlayEntrance(Mathf.Clamp(starCount, 0, stars.Length), shownReward));
        }

        public void Hide()
        {
            CancelEntrance();
            if (root != null) root.SetActive(false);
        }

        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        // The panel springs in over a fading dimmer, then the title, the three stars (earned ones with a burst),
        // the coin bar (counting up), the caption and finally the buttons arrive one after another.
        private IEnumerator PlayEntrance(int earnedStars, int reward)
        {
            float timing = Mathf.Clamp(sequenceTimeScale, 0.35f, 1f) * 1.35f;
            CanvasGroup dim = Dimmer();
            RectTransform[] buttonRects = ButtonRects();
            HideForEntrance(panel);
            HideForEntrance(titleArtwork);
            HideForEntrance(captionArtwork);
            HideForEntrance(coinBoard);
            CacheScale(coinReward);
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].gameObject.SetActive(true);
                stars[i].material = i < earnedStars ? null : greyStarMaterial;
                HideForEntrance(stars[i].transform);
            }
            foreach (RectTransform rect in buttonRects) HideForEntrance(rect);
            SetReward(0);
            countedReward = 0;
            if (coinBoard != null) coinBoard.gameObject.SetActive(true);
            if (coinReward != null) coinReward.gameObject.SetActive(true);
            SoundManager.EnsureInstance().PlaySfxWithMusicDuck("Win");
            if (ReducedMotion)
            {
                if (dim != null) dim.alpha = 1f;
                foreach (var entry in new List<KeyValuePair<Transform, Vector3>>(restingScales)) if (entry.Key != null) entry.Key.localScale = entry.Value;
                SetReward(reward);
                buttons.Set(1f, true);
                entrance = null;
                yield break;
            }

            float starStart = .55f * timing, starGap = .26f * timing;
            float boardStart = starStart + stars.Length * starGap;
            float countStart = boardStart + .12f * timing;
            float countDuration = Mathf.Max(.1f, coinCountDuration * timing);
            float captionStart = boardStart + .22f * timing;
            float buttonStart = captionStart + .20f * timing;
            float buttonGap = .12f * timing;
            float end = Mathf.Max(countStart + countDuration, buttonStart + buttonGap * 2f + .35f * timing);
            bool[] burst = new bool[stars.Length];
            float elapsed = 0f;
            while (elapsed < end)
            {
                elapsed += Time.unscaledDeltaTime;
                if (dim != null) dim.alpha = Mathf.Clamp01(elapsed / (.25f * timing));
                AnimatePop(panel, elapsed, .5f * timing, spring: .12f);
                AnimatePop(titleArtwork, elapsed - .28f * timing, .5f * timing, spring: .2f);
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null) continue;
                    float starTime = elapsed - (starStart + i * starGap);
                    AnimatePop(stars[i].transform, starTime, .55f * timing, spring: i < earnedStars ? .28f : .1f, jelly: i < earnedStars);
                    if (i < earnedStars && starTime > 0f && !burst[i])
                    {
                        burst[i] = true;
                        UiBurst.Play(this, panel, panel.InverseTransformPoint(stars[i].transform.position), new Color(1f, .9f, .4f, 1f), 9, 170f, .6f);
                        ButtonSfx.PlayCoinCountTick();
                    }
                }
                AnimatePop(coinBoard, elapsed - boardStart, .5f * timing, spring: .2f);
                float countT = Mathf.Clamp01((elapsed - countStart) / countDuration);
                SetCountedReward(Mathf.RoundToInt(Mathf.Lerp(0f, reward, 1f - Mathf.Pow(1f - countT, 3f))));
                AnimatePop(captionArtwork, elapsed - captionStart, .4f * timing, spring: .08f);
                for (int i = 0; i < buttonRects.Length; i++)
                    AnimatePop(buttonRects[i], elapsed - (buttonStart + i * buttonGap), .4f * timing, spring: .12f);
                float buttonT = Mathf.Clamp01((elapsed - buttonStart) / (.3f * timing));
                buttons.Set(buttonT * buttonT * (3f - 2f * buttonT), buttonT >= 1f && elapsed >= buttonStart + buttonGap * (buttonRects.Length - 1) + .2f * timing);
                yield return null;
            }
            SetReward(reward);
            buttons.Set(1f, true);
            // Rest state: earned stars keep a slow shine pulse and the Next button breathes.
            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                for (int i = 0; i < earnedStars && i < stars.Length; i++)
                    if (stars[i] != null && restingScales.TryGetValue(stars[i].transform, out Vector3 rest))
                        stars[i].transform.localScale = rest * (1f + .045f * Mathf.Sin(time * 3.4f + i * .9f));
                if (continueButton != null && restingScales.TryGetValue(continueButton.transform, out Vector3 restNext))
                    continueButton.transform.localScale = restNext * (1f + .025f * Mathf.Sin(time * 4.2f));
                yield return null;
            }
        }

        private CanvasGroup Dimmer()
        {
            Transform dimmer = FindChild(root.transform, "Dimmer");
            if (dimmer == null) return null;
            CanvasGroup group = dimmer.GetComponent<CanvasGroup>();
            if (group == null) group = dimmer.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            return group;
        }

        private RectTransform[] ButtonRects()
        {
            var list = new List<RectTransform>(2);
            if (continueButton != null) list.Add((RectTransform)continueButton.transform);
            if (homeButton != null) list.Add((RectTransform)homeButton.transform);
            return list.ToArray();
        }


        private void AnimatePop(Transform target, float elapsed, float duration, float spring = .16f, bool jelly = false)
        {
            if (target == null) return;
            CacheScale(target);
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration));
            float grow = 1f - Mathf.Pow(1f - t, 3f);
            float wobble = Mathf.Sin(t * Mathf.PI) * (1f - t) * spring;
            target.localScale = Vector3.Scale(restingScales[target], jelly
                ? new Vector3(grow * (1f + wobble), grow * (1f - wobble * .75f), 1f)
                : Vector3.one * (grow * (1f + wobble)));
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
            }
            if (action != null) StopCoroutine(action);
            action = null;
            actionPending = false;
            buttons.Restore();
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
            if (rewardText != null) rewardText.text = "+" + amount;
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
            if (homeButton == null) homeButton = FindButton(root.transform, "Home");
            if (captionArtwork == null) captionArtwork = FindChild(root.transform, "Caption") as RectTransform;
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
            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(Home);
                if (homeButton.onClick.GetPersistentEventCount() == 0) homeButton.onClick.AddListener(Home);
            }
        }

        /// <summary>Plays a rewarded ad, then pays the level's coins again and counts the board up to the doubled total.</summary>
        public void ClaimDoubleCoins()
        {
            if (actionPending || !buttons.Ready || doubleCoinsClaimed || shownReward <= 0 || doubleCoinsButton == null || RewardedAds.IsRequestPending) return;
            BeginAction(() =>
            {
                int version = presentationVersion;
                int reward = shownReward;
                AvnEvents.Navigation("double_reward", "level_complete");
                RewardedAds.Show(DoubleCoinsPlacement,
                    onRewarded: () =>
                    {
                        GameSystem.Instance?.AwardCoins(reward);
                        GameAnalytics.CurrencyEarned(reward, GameAnalytics.EarnAdDouble, GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0);
                        AvnEvents.EconomyEarned(reward, GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0, EconomySource.rewarded_ad, EconomyDetail.level_complete_x2);
                        AvnEvents.LevelDoubleReward();
                        if (!IsOpen || version != presentationVersion) return;
                        doubleCoinsClaimed = true;
                        doubleCoinsButton.gameObject.SetActive(false);
                        actionPending = false;
                        action = StartCoroutine(PlayDoubledReward());
                    },
                    onFailed: outcome =>
                    {
                        if (!IsOpen || version != presentationVersion) return;
                        actionPending = false;
                        action = StartCoroutine(buttons.Fade(true));
                    });
            });
        }

        private void BeginAction(System.Action callback)
        {
            if (actionPending || !buttons.Ready || !IsOpen) return;
            actionPending = true;
            if (action != null) StopCoroutine(action);
            action = null;
            ButtonSfx.PlayClick();
            if (entrance != null) StopCoroutine(entrance);
            entrance = null;
            if (!doubleCoinsClaimed) SetReward(shownReward);
            RestoreScale(coinReward);
            action = StartCoroutine(FadeThenAct(callback));
        }

        private IEnumerator FadeThenAct(System.Action callback)
        {
            yield return buttons.Fade(false);
            callback();
        }

        private IEnumerator PlayDoubledReward()
        {
            int from = shownReward, to = shownReward * 2;
            float duration = Mathf.Max(0.1f, coinCountDuration * Mathf.Clamp(sequenceTimeScale, 0.35f, 1f));
            yield return buttons.Fade(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetCountedReward(Mathf.RoundToInt(Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t, 3f))));
                if (coinReward != null && restingScales.ContainsKey(coinReward))
                    coinReward.localScale = restingScales[coinReward] * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);

                yield return null;
            }
            SetReward(to);
            RestoreScale(coinReward);
            action = null;
        }
        public void SkipLevel() => BeginAction(() => { AvnEvents.Navigation("skip", "level_complete"); GameSystem.Instance?.SkipLevel(); });
        private void Next() => BeginAction(() => { AvnEvents.Navigation("next", "level_complete"); GameSystem.Instance?.NextLevel(); });
        private void Home() => BeginAction(() => { AvnEvents.Navigation("home", "level_complete"); GameSystem.Instance?.GoHome(); });
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
