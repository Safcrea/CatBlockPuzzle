using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Pages the production twelve-card layout over the full campaign.</summary>
    public sealed class PagedLevelSelection : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Serializable] public sealed class Slot
        {
            public GameObject root;
            public Button button;
            public Image artwork;
            public TMP_Text number;
            public Image[] stars;
        }
        [SerializeField] private CatBlockPuzzleGame gameplay;
        [SerializeField] private Slot[] slots = Array.Empty<Slot>();
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite filledStar;
        [SerializeField] private Sprite emptyStar;
        private int page;
        private Vector2 pressPosition;
        private int PageCount => gameplay != null && slots.Length > 0
            ? Mathf.Max(1, Mathf.CeilToInt(gameplay.LevelCount / (float)slots.Length)) : 1;

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => Play(slot));
            }
            if (previousButton != null) previousButton.onClick.AddListener(Previous);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
        }
        private void OnEnable() => Refresh();
        public void Show()
        {
            if (gameplay != null && slots.Length > 0) page = gameplay.RecommendedLevelIndex / slots.Length;
            gameObject.SetActive(true);
            Refresh();
            MenuTransition.Show(gameObject, true);
        }
        private void Previous() => ChangePage(-1);
        private void Next() => ChangePage(1);
        private void ChangePage(int direction)
        {
            int next = Mathf.Clamp(page + direction, 0, PageCount - 1);
            if (next == page) return;
            ButtonSfx.PlayClick();
            page = next;
            Refresh();
        }
        private void Play(int slot)
        {
            int index = page * slots.Length + slot;
            if (gameplay == null || !gameplay.IsLevelAvailable(index)) return;
            ButtonSfx.PlayClick();
            GameAnalytics.SetLevelStartSource(GameAnalytics.SourceLevelSelect);
            int number = EndlessLevels.IsActive && index == gameplay.RecommendedLevelIndex
                ? gameplay.RecommendedLevelNumber : index + 1;
            GameSystem.Instance?.StartLevelNumber(number);
        }
        private void Refresh()
        {
            if (gameplay == null || slots.Length == 0) return;
            page = Mathf.Clamp(page, 0, PageCount - 1);
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                int index = page * slots.Length + i;
                bool exists = index < gameplay.LevelCount;
                if (slot.root != null) slot.root.SetActive(exists);
                if (!exists) continue;
                bool available = gameplay.IsLevelAvailable(index);
                bool recommended = index == gameplay.RecommendedLevelIndex;
                if (slot.button != null)
                {
                    slot.button.interactable = available;
                    // Locked cats already have their own authored state and padlock.
                    var colors = slot.button.colors;
                    colors.disabledColor = Color.white;
                    slot.button.colors = colors;
                }
                if (slot.artwork != null)
                {
                    slot.artwork.sprite = !available ? lockedSprite : recommended ? selectedSprite : completedSprite;
                    slot.artwork.rectTransform.sizeDelta = recommended && available
                        ? new Vector2(446.46f, 383.54f) : new Vector2(296.11f, 327.27f);
                }
                if (slot.number != null)
                {
                    slot.number.text = (EndlessLevels.IsActive && recommended ? gameplay.RecommendedLevelNumber : index + 1).ToString();
                    slot.number.gameObject.SetActive(available);
                }
                int stars = gameplay.GetLevelStars(index);
                if (slot.stars != null) for (int s = 0; s < slot.stars.Length; s++)
                {
                    if (slot.stars[s] == null) continue;
                    slot.stars[s].gameObject.SetActive(available && !recommended);
                    slot.stars[s].sprite = s < stars ? filledStar : emptyStar;
                }
            }
            if (coinLabel != null) coinLabel.text = gameplay.CurrentCoins.ToString();
            if (pageLabel != null) pageLabel.text = (page + 1) + " / " + PageCount;
            if (previousButton != null) previousButton.interactable = page > 0;
            if (nextButton != null) nextButton.interactable = page < PageCount - 1;
        }
        public void OnBeginDrag(PointerEventData eventData) => pressPosition = eventData.position;
        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - pressPosition;
            if (Mathf.Abs(delta.x) > 60f && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) ChangePage(delta.x < 0f ? 1 : -1);
        }
    }
}
