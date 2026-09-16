using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;

/// <summary>A compact authoring view; generated bookkeeping stays serialized but out of the way.</summary>
[CustomEditor(typeof(CatPuzzleContentCatalog))]
public sealed class CatPuzzleContentCatalogEditor : Editor
{
    private bool showLevels;
    private bool showRooms;
    private bool showArt;
    private int selectedLevel;
    private int selectedRoom;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var levels = serializedObject.FindProperty("levelPack").FindPropertyRelative("levels");
        var rooms = serializedObject.FindProperty("metaData").FindPropertyRelative("chapters");

        EditorGUILayout.LabelField("Game Content", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{levels.arraySize} levels  •  {rooms.arraySize} rooms", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        showLevels = EditorGUILayout.Foldout(showLevels, "Levels", true);
        if (showLevels && levels.arraySize > 0)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var level = SelectEntry(levels, "Level", ref selectedLevel);
                Field(level, "title", "Name");
                Field(level, "difficulty", "Difficulty");
                Field(level, "reward", "Coin reward");
                EditorGUILayout.PropertyField(level.FindPropertyRelative("pieces"), new GUIContent("Puzzle pieces"), true);
                Field(level, "rows", "Board rows");
                Field(level, "cols", "Board columns");
            }
        }

        showRooms = EditorGUILayout.Foldout(showRooms, "Rooms", true);
        if (showRooms && rooms.arraySize > 0)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var room = SelectEntry(rooms, "Room", ref selectedRoom);
                Field(room, "title", "Name");
                Field(room, "catName", "Cat name");
                Field(room, "startStory", "Welcome message");
                Field(room, "completionStory", "Completion message");
                Field(room, "catPortraitIndex", "Cat portrait index");
                // Stored indices are zero-based; display the level numbers players see.
                LevelNumber(room, "firstLevelIndex", "First level");
                LevelNumber(room, "lastLevelIndex", "Last level");
                EditorGUILayout.PropertyField(room.FindPropertyRelative("decorations"), new GUIContent("Furniture"), true);
            }
        }

        showArt = EditorGUILayout.Foldout(showArt, "Artwork", true);
        if (showArt)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roomBackgrounds"), new GUIContent("Room backgrounds"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roomThumbnails"), new GUIContent("Room preview images"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("decorations"), new GUIContent("Furniture images"), true);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private static SerializedProperty SelectEntry(SerializedProperty array, string label, ref int selected)
    {
        var names = new string[array.arraySize];
        for (int i = 0; i < names.Length; i++)
            names[i] = $"{i + 1}. {array.GetArrayElementAtIndex(i).FindPropertyRelative("title").stringValue}";
        selected = EditorGUILayout.Popup(label, Mathf.Clamp(selected, 0, names.Length - 1), names);
        return array.GetArrayElementAtIndex(selected);
    }

    private static void Field(SerializedProperty parent, string name, string label)
    {
        EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label));
    }

    private static void LevelNumber(SerializedProperty parent, string name, string label)
    {
        var property = parent.FindPropertyRelative(name);
        EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.IntField(label, property.intValue + 1);
        if (EditorGUI.EndChangeCheck()) property.intValue = Mathf.Max(1, value) - 1;
    }
}
