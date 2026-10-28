using UnityEngine;

namespace CatBlocksUI {
    [RequireComponent(typeof(UnityEngine.UI.Toggle), typeof(UnityEngine.UI.Image))]
    public sealed class ToggleSpriteState : MonoBehaviour {
        [SerializeField] Sprite enabledSprite;
        [SerializeField] Sprite disabledSprite;
        [SerializeField] UnityEngine.UI.Image knobImage;
        [SerializeField] Sprite enabledKnobSprite,disabledKnobSprite;
        [SerializeField] LayoutRect enabledKnobRect,disabledKnobRect;
        UnityEngine.UI.Toggle toggle;
        UnityEngine.UI.Image image;
        void OnEnable(){toggle=GetComponent<UnityEngine.UI.Toggle>();image=GetComponent<UnityEngine.UI.Image>();toggle.onValueChanged.AddListener(Apply);Apply(toggle.isOn);}
        void OnDisable(){if(toggle)toggle.onValueChanged.RemoveListener(Apply);}
        public void Configure(Sprite enabled,Sprite disabled){enabledSprite=enabled;disabledSprite=disabled;if(!toggle)toggle=GetComponent<UnityEngine.UI.Toggle>();if(!image)image=GetComponent<UnityEngine.UI.Image>();Apply(toggle.isOn);}
        public void ConfigureKnob(UnityEngine.UI.Image knob,Sprite enabled,Sprite disabled,LayoutRect onRect,LayoutRect offRect){knobImage=knob;enabledKnobSprite=enabled;disabledKnobSprite=disabled;enabledKnobRect=onRect;disabledKnobRect=offRect;Apply(toggle.isOn);}
        void Apply(bool enabled){var sprite=enabled?enabledSprite:disabledSprite;if(image&&sprite)image.sprite=sprite;if(!knobImage)return;knobImage.sprite=enabled?enabledKnobSprite:disabledKnobSprite;var r=enabled?enabledKnobRect:disabledKnobRect;if(r==null)return;var rt=knobImage.rectTransform;var parent=rt.parent as RectTransform;if(!parent)return;var size=parent.rect.size;rt.sizeDelta=r.size-Vector2.Scale(size,rt.anchorMax-rt.anchorMin);var position=new Vector2(r.x+r.width*rt.pivot.x,size.y-r.y-r.height*(1-rt.pivot.y));var anchor=Vector2.Scale(size,rt.anchorMin+Vector2.Scale(rt.anchorMax-rt.anchorMin,rt.pivot));rt.anchoredPosition=position-anchor;}
    }
}
