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
        private int selectedLevel = -1;
        public int SelectedLevel => selectedLevel;
        public int SlotCount => levels != null ? levels.Length : 0;

        private void OnEnable()
        {
            if (levels == null) return;
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
            selectionText.text = "Level " + (selectedLevel + 1);
            playButton.interactable = gameplay.IsLevelAvailable(selectedLevel);
        }
        public void Play() => GameSystem.Instance?.StartLevel(selectedLevel);
    }
}
