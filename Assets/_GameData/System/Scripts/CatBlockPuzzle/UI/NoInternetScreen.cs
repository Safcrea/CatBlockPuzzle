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
            AVNEvents.AvnEvents.PopupOpened(AVNEvents.AvnScreen.no_internet, root);
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void Retry()
        {
            ButtonSfx.PlayClick();
            AVNEvents.AvnEvents.Navigation(Application.internetReachability != NetworkReachability.NotReachable
                ? "retry_connected" : "retry_offline", "no_internet");
            if (Application.internetReachability != NetworkReachability.NotReachable) Hide();
        }
        private void OnOkayClicked()
        {
            ButtonSfx.PlayClick();
            AVNEvents.AvnEvents.Navigation("okay", "no_internet");
            AVNEvents.AvnEvents.PopupClosed(AVNEvents.AvnScreen.no_internet, AVNEvents.ScreenClosedBy.Dismissed);
            Hide();
        }
    }
}
