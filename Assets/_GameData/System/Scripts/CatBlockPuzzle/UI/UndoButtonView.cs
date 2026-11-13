using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The Undo button on the tray: takes back the last cat move. The badge shows the free undos left this attempt, then
    /// the coin price (with a coin) once they are used up. Greyed out while there is nothing to undo.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UndoButtonView : MonoBehaviour
    {
        [SerializeField] private Text badge;
        [SerializeField] private Image coin;
        [SerializeField] private CanvasGroup group;
        private Button button;
        private UndoController bound;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Click);
        }

        private void OnDisable() { if (bound != null) bound.Changed -= Refresh; bound = null; }

        // Binds once the gameplay controller exists, and refreshes every frame it could have changed.
        private void LateUpdate()
        {
            UndoController undo = GameSystem.Instance != null ? GameSystem.Instance.Undo : null;
            if (undo != bound)
            {
                if (bound != null) bound.Changed -= Refresh;
                bound = undo;
                if (bound != null) bound.Changed += Refresh;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (bound == null) return;
            int free = bound.FreeLeft;
            if (badge != null) badge.text = free > 0 ? free.ToString() : UndoController.CoinPrice.ToString();
            if (coin != null) coin.gameObject.SetActive(free <= 0);
            bool active = bound.HasMove;
            if (group != null) group.alpha = active ? 1f : .6f;
        }

        private void Click()
        {
            UndoController undo = GameSystem.Instance != null ? GameSystem.Instance.Undo : null;
            if (undo == null) return;
            switch (undo.TryUndo())
            {
                case UndoController.Result.Undone: ButtonSfx.PlayClick(); break;
                case UndoController.Result.NothingToUndo: ButtonSfx.PlayError(); GameSystem.Instance.Toast?.Show("Nothing to undo yet"); break;
                case UndoController.Result.NotEnoughCoins: ButtonSfx.PlayError(); GameSystem.Instance.Toast?.Show("Undo costs " + UndoController.CoinPrice + " coins"); break;
                default: ButtonSfx.PlayError(); break;
            }
            Refresh();
        }
    }
}
