using System;
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
        [Header("Chapter Unfold")]
        [Tooltip("Scroll view holding the chapters. Falls back to a child named 'Chapter Board'.")]
        [SerializeField] private RectTransform chapterBoard;
        [Tooltip("On: each chapter panel swings open in turn. Off: the screen just springs in.")]
        [SerializeField] private bool unfoldChapters = true;
        [SerializeField] private MenuTransition.EntranceSettings openingAnimation = new MenuTransition.EntranceSettings
        {
            duration = .4f,
            startingScale = .94f,
            slideDistance = 26f,
            overshoot = 1.5f,
            // Chapters begin swinging open while the board is still settling.
            cardDelay = .16f,
            cardStagger = .11f,
            cardDuration = .46f,
            cardStartingScale = .84f,
            cardSlideDistance = 34f,
            cardTilt = 0f,
            cardUnfoldAngle = 84f,
            unfoldFromTop = true
        };
        [SerializeField] private MenuTransition.ExitSettings closingAnimation = new MenuTransition.ExitSettings
        {
            duration = .24f,
            endingScale = .9f,
            slideDistance = 40f,
            anticipation = 1.1f
        };
        private int selectedLevel = -1;
        private bool bindingsValidated;
        private Transform[] cards;
        public int SelectedLevel => selectedLevel;
        public int SlotCount => levels != null ? levels.Length : 0;
        public bool IsOpen => gameObject.activeInHierarchy;

        /// <summary>Springs the screen in, then unfolds each chapter panel in turn.</summary>
        public void Show()
        {
            MenuTransition.Show(gameObject, true, openingAnimation, BuildCards());
        }

        public void Hide() => MenuTransition.Hide(gameObject, null, false);
        public void HideAnimated(System.Action completed) => MenuTransition.Hide(gameObject, completed, true, closingAnimation);

        private RectTransform ResolveChapterBoard()
        {
            if (chapterBoard == null) chapterBoard = transform.Find("Chapter Board") as RectTransform;
            return chapterBoard;
        }

        /// <summary>
        /// Chapter panels in board order, then the play button as the closing beat. The panels sit inside the
        /// scroll view's RectMask2D, which clips by an axis-aligned rect - so they carry the unfold themselves
        /// rather than the board rotating above the mask.
        /// </summary>
        private Transform[] BuildCards()
        {
            if (cards != null) return cards;
            var parts = new System.Collections.Generic.List<Component>();
            RectTransform content = ResolveChapterContent();
            // The tiles themselves are never cards: a hundred of them would stagger for several seconds.
            if (unfoldChapters && content != null)
                for (int i = 0; i < content.childCount; i++) parts.Add(content.GetChild(i));
            if (playButton != null) parts.Add(playButton);
            cards = MenuTransition.Cards(parts.ToArray());
            return cards;
        }

        private RectTransform ResolveChapterContent()
        {
            RectTransform board = ResolveChapterBoard();
            if (board == null) return null;
            var scroll = board.GetComponent<ScrollRect>();
            return scroll != null ? scroll.content : board.Find("Viewport/Chapters") as RectTransform;
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
            Refresh();
        }
        private void OnDisable()
        {
            if (levels != null) foreach (var slot in levels) if (slot.button != null && slot.click != null) slot.button.onClick.RemoveListener(slot.click);
            if (playButton != null) playButton.onClick.RemoveListener(Play);
        }
        public void Select(int index)
        {
            if (gameplay == null || !gameplay.IsLevelAvailable(index)) return;
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
            coinText.text = gameplay.CurrentCoins.ToString();
            selectionText.text = selectedLevel >= 0 ? "Level " + (selectedLevel + 1) : "No level available";
            playButton.interactable = gameplay.IsLevelAvailable(selectedLevel);
        }
        public void Play() => GameSystem.Instance?.StartLevel(selectedLevel);

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
