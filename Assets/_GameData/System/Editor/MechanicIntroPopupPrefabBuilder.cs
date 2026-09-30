using System.IO;
using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds Resources/CatBlockPuzzle/MechanicIntroPopup.prefab from the power-up purchase popup, so the
/// mechanic intro cards share its panel art, fonts and wooden button. Re-run after restyling that popup.
/// </summary>
public static class MechanicIntroPopupPrefabBuilder
{
    private const string SourcePath = "Assets/Resources/CatBlockPuzzle/PowerUpPurchasePopup.prefab";
    private const string TargetPath = "Assets/Resources/CatBlockPuzzle/MechanicIntroPopup.prefab";

    [MenuItem("Cat Block Puzzle/Build Mechanic Intro Popup Prefab")]
    public static void Build()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) throw new FileNotFoundException("Missing " + SourcePath);

        GameObject copy = Object.Instantiate(source);
        try
        {
            copy.name = "MechanicIntroPopup";
            Object.DestroyImmediate(copy.GetComponent<PowerUpPurchasePopup>());
            RectTransform panel = (RectTransform)copy.transform.Find("Panel");
            Object.DestroyImmediate(panel.Find("Wallet").gameObject);
            Object.DestroyImmediate(panel.Find("Close").gameObject);

            RectTransform title = (RectTransform)panel.Find("Title");
            title.GetComponent<Text>().text = "New!";
            RectTransform icon = (RectTransform)panel.Find("Power Up Icon");
            icon.name = "Mechanic Icon";

            // The message takes the wallet's space; the wooden button moves into the close button's slot.
            RectTransform message = (RectTransform)panel.Find("Message");
            message.anchoredPosition = new Vector2(0f, -35f);
            message.sizeDelta = new Vector2(540f, 190f);
            Text messageText = message.GetComponent<Text>();
            messageText.resizeTextForBestFit = true;
            messageText.resizeTextMinSize = 22;
            messageText.resizeTextMaxSize = messageText.fontSize;
            messageText.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform button = (RectTransform)panel.Find("Buy");
            button.name = "Continue";
            button.anchoredPosition = new Vector2(0f, -220f);
            button.sizeDelta = new Vector2(380f, 82f);
            Button continueButton = button.GetComponent<Button>();
            continueButton.onClick = new Button.ButtonClickedEvent();
            button.GetComponentInChildren<Text>(true).text = "Got it!";

            MechanicIntroPopup popup = copy.AddComponent<MechanicIntroPopup>();
            SerializedObject serialized = new SerializedObject(popup);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("heading").objectReferenceValue = title.GetComponent<Text>();
            serialized.FindProperty("message").objectReferenceValue = messageText;
            serialized.FindProperty("icon").objectReferenceValue = icon.GetComponent<Image>();
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            copy.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(copy, TargetPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + TargetPath);
            Debug.Log("CAT_MECHANIC_INTRO_PREFAB_BUILT " + TargetPath);
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }
}
