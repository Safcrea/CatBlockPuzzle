using UnityEngine;
using TMPro;

namespace CatBlocksUI {
    [DisallowMultipleComponent]
    public sealed class ScreenLayout : MonoBehaviour {
        public string screenId;
        public TextAsset sourceLayout;
        [HideInInspector] public string sourceAssetRoot;
        [Tooltip("Dynamic text is independent of button artwork. Binding names are defined by each layout JSON.")]
        public TextMeshProUGUI[] dynamicText;

        public bool SetText(string binding, string value) {
            bool found=false;
            foreach(var text in dynamicText) {
                if(!text)continue;
                var key=text.GetComponent<LayoutElement>();
                if(key && key.binding==binding){text.text=value;found=true;}
            }
            return found;
        }
        public bool SetToggle(string binding,bool enabled){
            foreach(var tag in GetComponentsInChildren<LayoutElement>(true)){if(tag.binding!=binding)continue;var toggle=tag.GetComponent<UnityEngine.UI.Toggle>();if(toggle){toggle.isOn=enabled;return true;}}
            return false;
        }
    }
}
