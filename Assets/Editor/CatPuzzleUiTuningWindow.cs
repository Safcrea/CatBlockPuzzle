using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;

public sealed class CatPuzzleUiTuningWindow : EditorWindow
{
    private const string ProfilePath = "Assets/Resources/CatBlockPuzzle/portrait_layout_profile.asset";
    private const string PreviewLevelKey = "CatBlockPuzzle.EditorPreviewLevel";
    private const string PreviewLevelPreference = "CatBlockPuzzle.UiTuning.PreviewLevel";

    private PortraitLayoutProfile profile;
    private SerializedObject serializedProfile;
    private Vector2 scroll;
    private int previewLevel;

    [MenuItem("Cat Block Puzzle/UI Tuning")]
    private static void OpenWindow()
    {
        GetWindow<CatPuzzleUiTuningWindow>("Cat Puzzle UI");
    }

    private void OnEnable()
    {
        previewLevel = Mathf.Clamp(EditorPrefs.GetInt(PreviewLevelPreference, 1), 1, 100);
        LoadOrCreateProfile();
    }

    private void OnGUI()
    {
        if (profile == null || serializedProfile == null)
        {
            LoadOrCreateProfile();
            if (profile == null)
            {
                EditorGUILayout.HelpBox("The UI tuning profile could not be created.", MessageType.Error);
                return;
            }
        }

        EditorGUILayout.LabelField("Portrait UI And Touch Tuning", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These values drive the runtime-generated gameplay UI. Save changes, then rebuild the selected level to preview them.",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        serializedProfile.Update();
        SerializedProperty property = serializedProfile.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath == "m_Script")
            {
                continue;
            }

            EditorGUILayout.PropertyField(property, true);
        }
        if (serializedProfile.ApplyModifiedProperties())
        {
            profile.ValidateValues();
            EditorUtility.SetDirty(profile);
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        previewLevel = EditorGUILayout.IntSlider("Level", previewLevel, 1, 100);
        EditorPrefs.SetInt(PreviewLevelPreference, previewLevel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Profile", GUILayout.Height(30f)))
            {
                SaveProfile();
            }

            if (GUILayout.Button(Application.isPlaying ? "Apply And Rebuild" : "Play And Preview", GUILayout.Height(30f)))
            {
                SaveProfile();
                PreviewSelectedLevel();
            }
        }

        if (GUILayout.Button("Reset All To Defaults", GUILayout.Height(26f)))
        {
            if (EditorUtility.DisplayDialog("Reset Cat Puzzle UI", "Reset every layout and input tuning value?", "Reset", "Cancel"))
            {
                Undo.RecordObject(profile, "Reset Cat Puzzle UI Tuning");
                profile.ResetToDefaults();
                profile.ValidateValues();
                EditorUtility.SetDirty(profile);
                serializedProfile.Update();
                SaveProfile();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadOrCreateProfile()
    {
        profile = AssetDatabase.LoadAssetAtPath<PortraitLayoutProfile>(ProfilePath);
        if (profile == null)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/CatBlockPuzzle");
            profile = CreateInstance<PortraitLayoutProfile>();
            profile.ResetToDefaults();
            profile.ValidateValues();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
        }

        serializedProfile = new SerializedObject(profile);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        string name = path.Substring(split + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private void SaveProfile()
    {
        serializedProfile.ApplyModifiedProperties();
        profile.ValidateValues();
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    private void PreviewSelectedLevel()
    {
        int zeroBasedLevel = Mathf.Clamp(previewLevel - 1, 0, 99);
        if (!Application.isPlaying)
        {
            EditorPrefs.SetInt(PreviewLevelKey, zeroBasedLevel);
            EditorApplication.isPlaying = true;
            return;
        }

        CatBlockPuzzleGame game = FindFirstObjectByType<CatBlockPuzzleGame>();
        if (game == null)
        {
            ShowNotification(new GUIContent("Runtime game is still starting. Try Apply And Rebuild again."));
            return;
        }

        game.PreviewLevelForTesting(zeroBasedLevel);
    }
}
