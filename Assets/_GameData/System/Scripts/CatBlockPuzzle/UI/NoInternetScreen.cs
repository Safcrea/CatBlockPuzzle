using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class NoInternetScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button okayButton;

        private void Awake() { if (okayButton != null) okayButton.onClick.AddListener(OnOkayClicked); }
        private void OnDestroy() { if (okayButton != null) okayButton.onClick.RemoveListener(OnOkayClicked); }
        public void Show()
        {
            if (root == null) return;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void OnOkayClicked() { ButtonSfx.PlayClick(); Hide(); }
    }
}
