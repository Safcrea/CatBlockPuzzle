using System;
using System.IO;
using System.Reflection;
using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;

/// <summary>One-time, editor-only migration of the existing procedural artwork into saved assets.</summary>
public static class CatPuzzleUiAssetBaker
{
    public const string Folder = "Assets/Resources/CatBlockPuzzle/AuthoredUI";
    public const string CatalogPath = Folder + "/UiAssets.asset";
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Cat Block Puzzle/Authoring/Bake UI Assets")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Bake UI assets in Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<CatPuzzleUiAssets>(CatalogPath) != null)
            throw new InvalidOperationException("UI assets already exist. No existing assets were overwritten.");
        if (AssetDatabase.IsValidFolder(Folder) && AssetDatabase.FindAssets("", new[] { Folder }).Length > 0)
            throw new InvalidOperationException("The destination folder already exists. Inspect its contents before baking.");

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/CatBlockPuzzle", "AuthoredUI");
        var temporary = new GameObject("UI Asset Bake Workspace");
        temporary.SetActive(false);
        temporary.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var game = temporary.AddComponent<CatBlockPuzzleGame>();
            var visualType = typeof(CatBlockPuzzleGame).GetField("visualCatalog", PrivateInstance);
            var visual = Resources.Load("CatBlockPuzzle/cat_visual_catalog", visualType.FieldType);
            if (visual == null)
            {
                visual = (UnityEngine.Object)visualType.FieldType.GetMethod("LoadOrCreate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
            visualType.SetValue(game, visual);
            var catalog = ScriptableObject.CreateInstance<CatPuzzleUiAssets>();
            catalog.White = SaveSprite(Call<Sprite>(game, "CreateSolidSprite", Color.white), "White");
            catalog.RoundedBox = SaveSprite(Call<Sprite>(game, "CreateRoundedBoxSprite"), "RoundedBox");
            catalog.Circle = SaveSprite(Call<Sprite>(game, "CreateCircleSprite"), "Circle");
            catalog.Coin = SaveSprite(Call<Sprite>(game, "CreateCoinSprite"), "Coin");
            catalog.CatHead = SaveSprite(Call<Sprite>(game, "CreateCatHeadSprite"), "CatHead");
            catalog.Mouth = SaveSprite(Call<Sprite>(game, "CreateMouthSprite"), "Mouth");
            catalog.Tail = SaveSprite(Call<Sprite>(game, "CreateTailSprite"), "Tail");
            catalog.Paw = SaveSprite(Call<Sprite>(game, "CreatePawSprite"), "Paw");
            catalog.Star = SaveSprite(Call<Sprite>(game, "CreateStarSprite", false), "Star");
            catalog.StarOutline = SaveSprite(Call<Sprite>(game, "CreateStarSprite", true), "StarOutline");
            catalog.Background = SaveSprite(Call<Sprite>(game, "CreateBackgroundSprite"), "Background");
            Type iconType = typeof(CatBlockPuzzleGame).GetNestedType("UiIcon", BindingFlags.NonPublic);
            for (int i = 0; i < catalog.Icons.Length; i++)
                catalog.Icons[i] = SaveSprite(Call<Sprite>(game, "CreateUiIconSprite", Enum.ToObject(iconType, i)), "Icon" + i);
            Call<object>(game, "LoadAuthoredCatPortraits");
            var portraits = (Sprite[,])typeof(CatBlockPuzzleGame).GetField("catPortraitSprites", PrivateInstance).GetValue(game);
            for (int mood = 0; mood < 3; mood++)
                for (int i = 0; i < 8; i++)
                    catalog.Portraits[mood * 8 + i] = SaveSprite(portraits[mood, i], "Cat" + mood + "_" + i);
            for (int i = 0; i < catalog.ThemeBackgrounds.Length; i++)
            {
                // The catalog repeats themes in five-level blocks; resolve each distinct index.
                for (int level = 0; level < 100; level++)
                {
                    var theme = CatPuzzleThemeCatalog.GetThemeForLevel(level);
                    if (theme.Index != i) continue;
                    catalog.ThemeBackgrounds[i] = SaveSprite(Call<Sprite>(game, "BakeThemeBackgroundSprite", theme), "Theme" + i);
                    break;
                }
                if (catalog.ThemeBackgrounds[i] == null) throw new InvalidOperationException("Missing theme " + i);
            }
            string[] names = { "Button", "Snap", "Wrong", "Win" };
            float[] seconds = { .07f, .1f, .16f, .42f };
            float[] volumes = { .22f, .28f, .22f, .24f };
            float[][] frequencies = { new[] { 520f, 660f }, new[] { 720f, 980f }, new[] { 210f, 140f }, new[] { 520f, 660f, 780f, 1040f } };
            for (int i = 0; i < names.Length; i++)
            {
                AudioClip clip = Call<AudioClip>(game, "CreateToneClip", names[i], seconds[i], volumes[i], frequencies[i]);
                try { catalog.Sounds[i] = SaveWave(clip, names[i]); }
                finally { UnityEngine.Object.DestroyImmediate(clip); }
            }
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssetIfDirty(catalog);
            if (!EditorUtility.IsPersistent(visual)) UnityEngine.Object.DestroyImmediate(visual);
            Debug.Log("Authored UI assets saved: " + CatalogPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }

    private static T Call<T>(CatBlockPuzzleGame target, string method, params object[] args)
    {
        MethodInfo info = typeof(CatBlockPuzzleGame).GetMethod(method, PrivateInstance);
        if (info == null) throw new MissingMethodException(method);
        object value = info.Invoke(target, args);
        return value == null ? default : (T)value;
    }

    private static Sprite SaveSprite(Sprite sprite, string name)
    {
        if (sprite == null) throw new InvalidOperationException("Missing source sprite: " + name);
        sprite.name = name;
        if (!EditorUtility.IsPersistent(sprite.texture))
        {
            sprite.texture.name = name + "Texture";
            AssetDatabase.CreateAsset(sprite.texture, Folder + "/" + name + "Texture.asset");
        }
        AssetDatabase.CreateAsset(sprite, Folder + "/" + name + ".asset");
        return sprite;
    }

    private static AudioClip SaveWave(AudioClip clip, string name)
    {
        string path = Folder + "/" + name + ".wav";
        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Could not read " + name);
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)clip.channels);
            writer.Write(clip.frequency); writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32767));
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
