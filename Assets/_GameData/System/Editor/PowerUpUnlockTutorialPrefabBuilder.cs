using System.IO;
using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds Resources/CatBlockPuzzle/PowerUpUnlockTutorial.prefab: the dim, centre icon, bouncing
/// message and pointing hand used by the guided power-up introduction. Re-run to restyle it.
/// </summary>
public static class PowerUpUnlockTutorialPrefabBuilder
{
    private const string TargetPath = "Assets/Resources/CatBlockPuzzle/PowerUpUnlockTutorial.prefab";
    private const string FontPath = "Assets/_GameData/Art/Font/fredoka-one.one-regular.ttf";
    private const string HandPath = "Assets/Art/CatBlockPuzzleUI/UI/Handpointer.png";

    [MenuItem("Cat Block Puzzle/Build Power Up Unlock Tutorial Prefab")]
    public static void Build()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        Sprite handSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandPath);
        Sprite glow = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (font == null || handSprite == null) throw new FileNotFoundException("Missing " + (font == null ? FontPath : HandPath));

        var root = new GameObject("PowerUpUnlockTutorial", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
        try
        {
            Stretch((RectTransform)root.transform);
            root.layer = LayerMask.NameToLayer("UI");

            RectTransform dimRect = Child(root.transform, "Dim");
            Stretch(dimRect);
            Image dimImage = dimRect.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.08f, 0.05f, 0.04f, 0.8f);
            CanvasGroup dim = dimRect.gameObject.AddComponent<CanvasGroup>();

            RectTransform icon = Child(root.transform, "Icon");
            icon.sizeDelta = new Vector2(230f, 230f);
            CanvasGroup iconGroup = icon.gameObject.AddComponent<CanvasGroup>();
            iconGroup.blocksRaycasts = false;
            RectTransform glowRect = Child(icon, "Glow");
            glowRect.sizeDelta = new Vector2(340f, 340f);
            Image glowImage = glowRect.gameObject.AddComponent<Image>();
            glowImage.sprite = glow;
            glowImage.color = new Color(1f, 0.93f, 0.7f, 0.35f);
            glowImage.raycastTarget = false;
            RectTransform iconImageRect = Child(icon, "Icon Image");
            iconImageRect.sizeDelta = new Vector2(230f, 230f);
            Image iconImage = iconImageRect.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform messageRect = Child(root.transform, "Message");
            messageRect.anchoredPosition = new Vector2(0f, -230f);
            messageRect.sizeDelta = new Vector2(860f, 200f);
            Text message = messageRect.gameObject.AddComponent<Text>();
            message.font = font;
            message.fontSize = 50;
            message.alignment = TextAnchor.UpperCenter;
            message.color = Color.white;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Overflow;
            message.raycastTarget = false;
            Outline outline = messageRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.19f, 0.10f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            messageRect.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            RectTransform hand = Child(root.transform, "Hand");
            hand.gameObject.AddComponent<Canvas>();
            RectTransform handImageRect = Child(hand, "Hand Pointer");
            handImageRect.sizeDelta = new Vector2(150f, 150f);
            handImageRect.pivot = new Vector2(0.14f, 0.88f);
            Image handImage = handImageRect.gameObject.AddComponent<Image>();
            handImage.sprite = handSprite;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;

            PowerUpUnlockTutorial tutorial = root.AddComponent<PowerUpUnlockTutorial>();
            var serialized = new SerializedObject(tutorial);
            serialized.FindProperty("dim").objectReferenceValue = dim;
            serialized.FindProperty("iconRoot").objectReferenceValue = icon;
            serialized.FindProperty("iconGroup").objectReferenceValue = iconGroup;
            serialized.FindProperty("iconImage").objectReferenceValue = iconImage;
            serialized.FindProperty("message").objectReferenceValue = message;
            serialized.FindProperty("hand").objectReferenceValue = hand;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("UI");
            PrefabUtility.SaveAsPrefabAsset(root, TargetPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + TargetPath);
            Debug.Log("CAT_POWERUP_UNLOCK_TUTORIAL_PREFAB_BUILT " + TargetPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static RectTransform Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
