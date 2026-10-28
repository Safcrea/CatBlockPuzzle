using UnityEngine;

namespace CatBlocksUI {
    [DisallowMultipleComponent]
    public sealed class LayoutElement : MonoBehaviour {
        public string elementId;
        public string binding;
        public bool previewOnly;
    }
}
