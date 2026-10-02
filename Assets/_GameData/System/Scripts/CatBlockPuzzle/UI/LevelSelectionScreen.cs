using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectionScreen : MonoBehaviour
    {
        [Serializable] public sealed class LevelSlot
        {
            public Button button;
            public Text number;
            public Image background;
            public GameObject locked;
            public Image[] stars;
            [NonSerialized] public UnityAction click;
        }
        [SerializeField] private CatBlockPuzzleGame gameplay;
        [SerializeField] private Button playButton;
        [SerializeField] private Text coinText;
        [SerializeField] private Text selectionText;
        [SerializeField] private LevelSlot[] levels;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite earnedStar;
        [SerializeField] private Sprite emptyStar;
        [Header("Opening")]
        [Tooltip("Scroll view holding the chapter cards. Falls back to the authored Chapters content.")]
        [SerializeField] private RectTransform chapterBoard;
        private int selectedLevel = -1;
        private bool bindingsValidated;
        private CanvasGroup screenGroup;
        private CanvasGroup backgroundGroup;
        private float backgroundAlpha = 1f;
        private Coroutine openingRoutine;
        private readonly List<EntranceItem> entranceItems = new List<EntranceItem>();
        private readonly List<ChapterLockBadge> chapterLocks = new List<ChapterLockBadge>();

        private sealed class EntranceItem
        {
            public RectTransform rect;
            public CanvasGroup group;
            public Vector2 restingPosition;
            public float restingAlpha;
        }
        public int SelectedLevel => selectedLevel;
        public int SlotCount => levels != null ? levels.Length : 0;
        public bool IsOpen => gameObject.activeInHierarchy;

        /// <summary>Fades in the scene, then lets chapter cards fall into place in sequence.</summary>
        public void Show()
        {
            CancelOpening();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            FocusCurrentLevel();
            if (MenuTransition.ReducedMotion)
            {
                RestoreOpeningVisuals();
                SetScreenInteractive(true);
                return;
            }

            PrepareOpeningVisuals();
            openingRoutine = StartCoroutine(PlayOpening());
        }

        public void Hide()
        {
            CancelOpening();
            gameObject.SetActive(false);
        }

        public void HideAnimated(System.Action completed)
        {
            Hide();
            completed?.Invoke();
        }

        /// <summary>Selects the level the player should play next and scrolls its chapter into view.</summary>
        private void FocusCurrentLevel()
        {
            if (gameplay == null) return;
            int current = gameplay.RecommendedLevelIndex;
            if (!gameplay.IsLevelAvailable(current)) return;
            selectedLevel = current;
            Refresh();
            ScrollToLevel(current);
        }

        private void ScrollToLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length || levels[index].button == null) return;
            RectTransform content = ResolveChapterContent();
            if (content == null) return;
            // The authored "chapter board" reference may be the scroll view or its content; the ScrollRect sits above the content either way.
            ScrollRect scroll = content.GetComponentInParent<ScrollRect>(true);
            if (scroll == null) return;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;

            RectTransform card = CardFor(index, content);
            if (card == null) return;

            FitChapterContent();
            Canvas.ForceUpdateCanvases();
            // Work in viewport space so a scaled content (narrow screens) still centres the card.
            scroll.StopMovement();
            float cardCentre = viewport.InverseTransformPoint(card.TransformPoint(card.rect.center)).y;
            content.anchoredPosition += Vector2.up * (viewport.rect.center.y - cardCentre);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition);
        }

        private RectTransform ResolveChapterBoard()
        {
            if (chapterBoard == null) chapterBoard = transform.Find("Chapter Board") as RectTransform;
            return chapterBoard;
        }

        private RectTransform ResolveChapterContent()
        {
            RectTransform board = ResolveChapterBoard();
            if (board == null) return null;
            var scroll = board.GetComponent<ScrollRect>();
            if (scroll != null && scroll.content != null) return scroll.content;
            if (board.name == "Chapters") return board;
            RectTransform content = board.Find("Viewport/Chapters") as RectTransform;
            return content != null ? content : board.Find("Chapters") as RectTransform;
        }

        private void PrepareOpeningVisuals()
        {
            if (screenGroup == null)
            {
                screenGroup = GetComponent<CanvasGroup>();
                if (screenGroup == null) screenGroup = gameObject.AddComponent<CanvasGroup>();
            }
            SetScreenInteractive(false);

            Transform background = transform.Find("Bg");
            if (background != null)
            {
                backgroundGroup = background.GetComponent<CanvasGroup>();
                if (backgroundGroup == null) backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();
                backgroundAlpha = backgroundGroup.alpha;
                backgroundGroup.alpha = 0f;
            }

            entranceItems.Clear();
            RectTransform chapterContent = ResolveChapterContent();
            if (chapterContent != null)
                for (int i = 0; i < chapterContent.childCount; i++) AddEntranceItem(chapterContent.GetChild(i) as RectTransform);
            AddEntranceItem(transform.Find("PlayButtonBG") as RectTransform);
            AddEntranceItem(playButton != null ? playButton.transform as RectTransform : null);

            foreach (EntranceItem item in entranceItems)
            {
                item.group.alpha = 0f;
                item.group.interactable = false;
                item.group.blocksRaycasts = false;
                item.rect.anchoredPosition = item.restingPosition + Vector2.up * 28f;
            }
        }

        private void AddEntranceItem(RectTransform rect)
        {
            if (rect == null || entranceItems.Exists(item => item.rect == rect)) return;
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
            entranceItems.Add(new EntranceItem
            {
                rect = rect,
                group = group,
                restingPosition = rect.anchoredPosition,
                restingAlpha = group.alpha
            });
        }

        private IEnumerator PlayOpening()
        {
            const float backgroundDuration = .28f;
            float elapsed = 0f;
            while (elapsed < backgroundDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (backgroundGroup != null) backgroundGroup.alpha = Mathf.Lerp(0f, backgroundAlpha, EaseOut(elapsed / backgroundDuration));
                yield return null;
            }
            if (backgroundGroup != null) backgroundGroup.alpha = backgroundAlpha;

            const float cardDuration = .32f;
            const float stagger = .065f;
            float totalDuration = Mathf.Max(0f, (entranceItems.Count - 1) * stagger) + cardDuration;
            elapsed = 0f;
            while (elapsed < totalDuration)
            {
                for (int i = 0; i < entranceItems.Count; i++)
                {
                    EntranceItem item = entranceItems[i];
                    float t = Mathf.Clamp01((elapsed - i * stagger) / cardDuration);
                    float motion = EaseOut(t);
                    item.group.alpha = Mathf.Lerp(0f, item.restingAlpha, motion);
                    item.rect.anchoredPosition = Vector2.Lerp(item.restingPosition + Vector2.up * 28f, item.restingPosition, motion);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreOpeningVisuals();
            SetScreenInteractive(true);
            openingRoutine = null;
        }

        private void CancelOpening()
        {
            if (openingRoutine != null) StopCoroutine(openingRoutine);
            openingRoutine = null;
            RestoreOpeningVisuals();
        }

        private void RestoreOpeningVisuals()
        {
            if (backgroundGroup != null) backgroundGroup.alpha = backgroundAlpha;
            foreach (EntranceItem item in entranceItems)
            {
                if (item.rect != null) item.rect.anchoredPosition = item.restingPosition;
                if (item.group != null)
                {
                    item.group.alpha = item.restingAlpha;
                    item.group.interactable = true;
                    item.group.blocksRaycasts = true;
                }
            }
        }

        private void SetScreenInteractive(bool interactive)
        {
            if (screenGroup == null) return;
            screenGroup.interactable = interactive;
            screenGroup.blocksRaycasts = interactive;
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private void OnEnable()
        {
            if (!ValidateBindings(out string error))
            {
                Debug.LogError("Level Selection Screen is not configured: " + error, this);
                enabled = false;
                return;
            }
            for (int i = 0; i < levels.Length; i++)
            {
                int index = i;
                LevelSlot slot = levels[i];
                if (slot.click == null) slot.click = () => Select(index);
                slot.button.onClick.RemoveListener(slot.click);
                slot.button.onClick.AddListener(slot.click);
            }
            playButton.onClick.RemoveListener(Play);
            playButton.onClick.AddListener(Play);
            InstallPageScrolling();
            FitChapterContent();
            Refresh();
        }

        /// <summary>
        /// Chapter cards are authored 930 wide; on narrow phones the framed viewport is smaller,
        /// so the whole list scales down uniformly instead of clipping at the frame.
        /// </summary>
        private void FitChapterContent()
        {
            RectTransform content = ResolveChapterContent();
            ScrollRect scroll = content != null ? content.GetComponentInParent<ScrollRect>(true) : null;
            if (scroll == null || content.childCount == 0) return;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
            float cardWidth = 0f;
            for (int i = 0; i < content.childCount; i++) cardWidth = Mathf.Max(cardWidth, ((RectTransform)content.GetChild(i)).rect.width);
            if (cardWidth <= 0f || viewport.rect.width <= 0f) return;
            float scale = Mathf.Min(1f, (viewport.rect.width - ChapterSidePadding * 2f) / cardWidth);
            content.localScale = new Vector3(scale, scale, 1f);
        }

        private const float ChapterSidePadding = 6f;

        /// <summary>
        /// The chapter list only fills the middle of the page; drags anywhere else (title, coins,
        /// bottom gradient, play button area) bubble up to the page root and scroll it too.
        /// </summary>
        private void InstallPageScrolling()
        {
            RectTransform content = ResolveChapterContent();
            ScrollRect scroll = content != null ? content.GetComponentInParent<ScrollRect>(true) : null;
            if (scroll == null) return;
            if (!TryGetComponent(out ScrollDragForwarder forwarder)) forwarder = gameObject.AddComponent<ScrollDragForwarder>();
            forwarder.Target = scroll;
            // A transparent full-page hit surface makes sure empty areas receive the touch at all.
            if (!TryGetComponent(out Graphic _))
            {
                var surface = gameObject.AddComponent<Image>();
                surface.color = Color.clear;
                surface.raycastTarget = true;
            }
        }

        public ChapterLockBadge ChapterLock(int chapterIndex) => chapterIndex >= 0 && chapterIndex < chapterLocks.Count ? chapterLocks[chapterIndex] : null;

        /// <summary>Puts a padlock and the stars still needed over every chapter card that is locked.</summary>
        private void RefreshChapterLocks()
        {
            RectTransform content = ResolveChapterContent();
            if (content == null) return;
            int chapterCount = (levels.Length + CatMetaCatalog.LevelsPerChapter - 1) / CatMetaCatalog.LevelsPerChapter;
            Image lockIcon = levels[0].locked.GetComponent<Image>();
            for (int chapter = 0; chapter < chapterCount; chapter++)
            {
                if (chapterLocks.Count <= chapter) chapterLocks.Add(null);
                if (chapterLocks[chapter] == null)
                {
                    RectTransform card = CardFor(chapter * CatMetaCatalog.LevelsPerChapter, content);
                    if (card == null) continue;
                    if (!card.TryGetComponent(out ChapterLockBadge badge)) badge = card.gameObject.AddComponent<ChapterLockBadge>();
                    chapterLocks[chapter] = badge;
                }

                Text title = chapterLocks[chapter].transform.Find("Chapter Title")?.GetComponent<Text>();
                chapterLocks[chapter].Show(!gameplay.IsChapterUnlocked(chapter),
                    gameplay.StarsEarnedBeforeChapter(chapter), gameplay.StarsRequiredForChapter(chapter),
                    lockIcon != null ? lockIcon.sprite : null, earnedStar, title != null ? title.font : null);
            }
        }

        /// <summary>The chapter card is the content child that holds this level's button.</summary>
        private RectTransform CardFor(int levelIndex, RectTransform content)
        {
            if (levelIndex < 0 || levelIndex >= levels.Length || levels[levelIndex].button == null) return null;
            RectTransform card = levels[levelIndex].button.transform as RectTransform;
            while (card != null && card.parent != content) card = card.parent as RectTransform;
            return card;
        }
        private void OnDisable()
        {
            CancelOpening();
            if (levels != null) foreach (var slot in levels) if (slot.button != null && slot.click != null) slot.button.onClick.RemoveListener(slot.click);
            if (playButton != null) playButton.onClick.RemoveListener(Play);
        }
        public void Select(int index)
        {
            if (gameplay == null || !gameplay.IsLevelAvailable(index)) return;
            ButtonSfx.PlayClick();
            selectedLevel = index;
            Refresh();
        }
        public void Refresh()
        {
            if (gameplay == null || levels == null) return;
            if (!gameplay.IsLevelAvailable(selectedLevel)) selectedLevel = gameplay.RecommendedLevelIndex;
            if (!gameplay.IsLevelAvailable(selectedLevel)) selectedLevel = -1;
            for (int i = 0; i < levels.Length; i++)
            {
                var slot = levels[i];
                bool available = gameplay.IsLevelAvailable(i);
                slot.button.interactable = available;
                slot.number.text = ((i % 10) + 1).ToString();
                slot.number.gameObject.SetActive(available);
                slot.locked.SetActive(!available);
                slot.background.sprite = !available ? lockedSprite : i == selectedLevel ? selectedSprite : normalSprite;
                int stars = gameplay.GetLevelStars(i);
                for (int s = 0; s < slot.stars.Length; s++)
                {
                    slot.stars[s].sprite = s < stars ? earnedStar : emptyStar;
                    slot.stars[s].color = Color.white;
                }
            }
            RefreshChapterLocks();
            coinText.text = gameplay.CurrentCoins.ToString();
            selectionText.text = selectedLevel >= 0 ? "Level " + (selectedLevel + 1) : "No level available";
            playButton.interactable = gameplay.IsLevelAvailable(selectedLevel);
        }
        public void Play() { ButtonSfx.PlayClick(); GameSystem.Instance?.StartLevel(selectedLevel); }

        private bool ValidateBindings(out string error)
        {
            if (bindingsValidated)
            {
                error = string.Empty;
                return true;
            }

            if (gameplay == null) { error = "Gameplay is not assigned."; return false; }
            if (playButton == null) { error = "Play Button is not assigned."; return false; }
            if (coinText == null || selectionText == null) { error = "Coin Text or Selection Text is not assigned."; return false; }
            if (levels == null || levels.Length == 0) { error = "No level slots are assigned."; return false; }
            if (normalSprite == null || selectedSprite == null || lockedSprite == null || earnedStar == null || emptyStar == null)
            {
                error = "One or more level-card sprites are not assigned.";
                return false;
            }
            for (int i = 0; i < levels.Length; i++)
            {
                LevelSlot slot = levels[i];
                if (slot == null || slot.button == null || slot.number == null || slot.background == null || slot.locked == null || slot.stars == null)
                {
                    error = "Level slot " + (i + 1) + " has incomplete references.";
                    return false;
                }
                if (slot.stars.Length != CatPuzzleResultCalculator.MaximumStars)
                {
                    error = "Level slot " + (i + 1) + " must have exactly three star images.";
                    return false;
                }
                for (int star = 0; star < slot.stars.Length; star++)
                    if (slot.stars[star] == null) { error = "Level slot " + (i + 1) + " has an unassigned star image."; return false; }
            }
            bindingsValidated = true;
            error = string.Empty;
            return true;
        }
    }
}
