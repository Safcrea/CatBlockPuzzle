using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class NoInternetScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button okayButton;
        [SerializeField] private Button retryButton;

        private void Awake()
        {
            if (okayButton != null) okayButton.onClick.AddListener(OnOkayClicked);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
        }
        private void OnDestroy()
        {
            if (okayButton != null) okayButton.onClick.RemoveListener(OnOkayClicked);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        }
        public void Show()
        {
            if (root == null) return;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void Retry()
        {
            ButtonSfx.PlayClick();
            if (Application.internetReachability != NetworkReachability.NotReachable) Hide();
        }
        private void OnOkayClicked() { ButtonSfx.PlayClick(); Hide(); }
    }
}
