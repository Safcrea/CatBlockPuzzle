using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Testing bar on the gameplay screen: previous, next and jump-to-level. Every load is a
    /// preview, so it ignores locks and never awards stars or coins or saves progress.
    /// GameSystem > Testing > Show Level Test Buttons decides whether the bar is active at all.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelTestNavigator : MonoBehaviour
    {
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private InputField levelInput;
        [SerializeField] private Button goButton;

        private int shownLevel = -1;
        private int shownCount = -1;

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(OnPreviousClicked);
            if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
            if (goButton != null) goButton.onClick.AddListener(OnGoClicked);
            if (levelInput != null) levelInput.onSubmit.AddListener(OnLevelSubmitted);
        }

        private void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(OnPreviousClicked);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
            if (goButton != null) goButton.onClick.RemoveListener(OnGoClicked);
            if (levelInput != null) levelInput.onSubmit.RemoveListener(OnLevelSubmitted);
        }

        private void OnEnable() => shownLevel = shownCount = -1;

        private void Update()
        {
            GameSystem system = GameSystem.Instance;
            int level = system != null ? system.CurrentLevelNumber : 1;
            int count = system != null ? system.LevelCount : 0;
            if (level == shownLevel && count == shownCount) return;
            shownLevel = level;
            shownCount = count;
            if (previousButton != null) previousButton.interactable = level > 1;
            if (nextButton != null) nextButton.interactable = level < count;
            if (levelInput != null && levelInput.placeholder is Text placeholder) placeholder.text = "1-" + Mathf.Max(1, count);
        }

        private void OnPreviousClicked() { ButtonSfx.PlayClick(); GameSystem.Instance?.LoadPreviousLevel(); }
        private void OnNextClicked() { ButtonSfx.PlayClick(); GameSystem.Instance?.LoadNextLevel(); }
        private void OnGoClicked() { ButtonSfx.PlayClick(); JumpToTypedLevel(); }
        private void OnLevelSubmitted(string _) => JumpToTypedLevel();

        private void JumpToTypedLevel()
        {
            GameSystem system = GameSystem.Instance;
            if (system == null || levelInput == null || system.LevelCount <= 0) return;
            if (!int.TryParse(levelInput.text, out int levelNumber)) return;
            levelNumber = Mathf.Clamp(levelNumber, 1, system.LevelCount);
            levelInput.text = levelNumber.ToString();
            system.PreviewLevel(levelNumber - 1);
        }
    }
}
