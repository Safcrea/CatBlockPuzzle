using System;
using System.IO;
using CatBlockPuzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level Plan v2 additions to the hand-authored GameScene, made in place instead of re-running
/// Prepare Game Scene: the Rotate power-up button beside Freeze and the Daily Puzzle button above
/// Daily Reward. Their art is drawn here too, on blank copies of the existing button faces.
/// Both menu items are safe to run again.
/// </summary>
public static class LevelPlanV2SceneAuthoring
{
    private const string RotateSpritePath = "Assets/Art/CatBlockPuzzleUI/UI/Slicing/Gameplay/rotate.png";
    private const string DailyPuzzleSpritePath = "Assets/Art/CatBlockPuzzleUI/UI/Slicing/MainMenu/daily_puzzle_button.png";
    private const string DoneMarkSpritePath = "Assets/Art/CatBlockPuzzleUI/UI/Slicing/MainMenu/daily_puzzle_done.png";
    private const string FreezeSourcePath = "Assets/Art/CatBlockPuzzleUI/UI/Slicing/Gameplay/time_freeze.png";
    private const string DailyRewardSourcePath = "Assets/PSD2UI Imports/layout/L_0-6_daily_reward_button_0005.png";
    private const string FontPath = "Assets/_GameData/Art/Font/fredoka-one.one-regular.ttf";

    // The brown of the baked menu-button labels.
    private static readonly Color LabelBrown = new Color32(0x7A, 0x3F, 0x24, 0xFF);

    [MenuItem("Cat Block Puzzle/Level Plan v2/Draw Button Art")]
    public static void DrawArt()
    {
        DrawRotateIcon();
        DrawDailyPuzzleButton();
        DrawDoneMark();
        Debug.Log("Level Plan v2 button art written: " + RotateSpritePath + ", " + DailyPuzzleSpritePath + ", " + DoneMarkSpritePath);
    }

    [MenuItem("Cat Block Puzzle/Level Plan v2/Add Rotate and Daily Puzzle Buttons (Keep Scene)")]
    public static void AddButtons()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(RotateSpritePath) == null) DrawArt();
        var hud = UnityEngine.Object.FindFirstObjectByType<GameplayHudView>(FindObjectsInactive.Include);
        var home = UnityEngine.Object.FindFirstObjectByType<HomeController>(FindObjectsInactive.Include);
        if (hud == null || home == null) throw new InvalidOperationException("Open GameScene first: it needs a GameplayHudView and a HomeController.");

        AddRotateButton(hud);
        AddDailyPuzzleButton(home);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
    }

    // ------------------------------------------------------------------ scene

    private static void AddRotateButton(GameplayHudView hud)
    {
        var so = new SerializedObject(hud);
        var freeze = so.FindProperty("freezeButton").objectReferenceValue as Button;
        if (freeze == null) throw new InvalidOperationException("GameplayHudView has no Freeze button to copy.");
        var rotate = so.FindProperty("rotateButton").objectReferenceValue as Button;
        if (rotate == null)
        {
            GameObject copy = UnityEngine.Object.Instantiate(freeze.gameObject, freeze.transform.parent);
            Undo.RegisterCreatedObjectUndo(copy, "Add Rotate Button");
            copy.name = "Rotate";
            copy.transform.SetSiblingIndex(freeze.transform.GetSiblingIndex() + 1);
            rotate = copy.GetComponent<Button>();
        }

        rotate.onClick = new Button.ButtonClickedEvent();
        ((Image)rotate.targetGraphic).sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RotateSpritePath);
        Transform board = rotate.transform.GetChild(0);
        board.name = "Background Rotate";
        Text count = board.GetComponentInChildren<Text>(true);
        count.name = "Rotate Count";

        so.FindProperty("rotateButton").objectReferenceValue = rotate;
        so.FindProperty("rotateCountText").objectReferenceValue = count;
        so.FindProperty("rotateCountBoard").objectReferenceValue = board.gameObject;
        so.ApplyModifiedProperties();
        // GameplayHudView spreads Hint, Freeze and Rotate at runtime once Rotate unlocks.
    }

    private static void AddDailyPuzzleButton(HomeController home)
    {
        var so = new SerializedObject(home);
        var reward = so.FindProperty("dailyRewardButton").objectReferenceValue as Button;
        var levels = so.FindProperty("levelsButton").objectReferenceValue as Button;
        if (reward == null || levels == null) throw new InvalidOperationException("HomeController needs its Daily Reward and Levels buttons.");
        var puzzle = so.FindProperty("dailyPuzzleButton").objectReferenceValue as Button;
        if (puzzle == null)
        {
            GameObject copy = UnityEngine.Object.Instantiate(reward.gameObject, reward.transform.parent);
            Undo.RegisterCreatedObjectUndo(copy, "Add Daily Puzzle Button");
            copy.name = "Daily Puzzle";
            copy.transform.SetSiblingIndex(reward.transform.GetSiblingIndex() + 1);
            for (int i = copy.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
            puzzle = copy.GetComponent<Button>();
        }

        puzzle.onClick = new Button.ButtonClickedEvent();
        ((Image)puzzle.targetGraphic).sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DailyPuzzleSpritePath);
        // Straight above Daily Reward, level with the Levels button.
        puzzle.transform.position = new Vector3(reward.transform.position.x, levels.transform.position.y, reward.transform.position.z);

        Text label = Child<Text>(puzzle.transform, "Label");
        label.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        label.text = "Daily Puzzle";
        label.color = LabelBrown;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 25;
        label.fontSize = 25;
        label.raycastTarget = false;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 20f);
        labelRect.sizeDelta = new Vector2(-24f, 30f);

        Image done = Child<Image>(puzzle.transform, "Done");
        done.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DoneMarkSpritePath);
        done.preserveAspect = true;
        done.raycastTarget = false;
        var doneRect = done.rectTransform;
        doneRect.anchorMin = doneRect.anchorMax = doneRect.pivot = new Vector2(0.5f, 0.5f);
        doneRect.anchoredPosition = new Vector2(64f, 58f);
        doneRect.sizeDelta = new Vector2(58f, 58f);
        done.gameObject.SetActive(false);

        so.FindProperty("dailyPuzzleButton").objectReferenceValue = puzzle;
        so.FindProperty("dailyPuzzleDoneMark").objectReferenceValue = done.gameObject;
        so.ApplyModifiedProperties();
    }

    private static T Child<T>(Transform parent, string name) where T : Graphic
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out T found)) return found;
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
        Undo.RegisterCreatedObjectUndo(go, "Add " + name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    // ------------------------------------------------------------------ art

    // A clockwise turn arrow on the Freeze button's tile.
    private static void DrawRotateIcon()
    {
        Color[] px = LoadPixels(FreezeSourcePath, out int w, out int h);
        BlankFace(px, w, h);
        Vector2 c = new Vector2(w * 0.5f, h * 0.5f + 1f);
        const float radius = 35f, thickness = 15f, start = 100f, sweep = 280f;
        float end = (start - sweep) * Mathf.Deg2Rad;
        var endDir = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
        var tangent = new Vector2(Mathf.Sin(end), -Mathf.Cos(end)); // clockwise travel
        Vector2 basePoint = c + endDir * radius - tangent * 2f;
        Vector2 tipA = basePoint + endDir * thickness * 1.35f;
        Vector2 tipB = basePoint - endDir * thickness * 1.35f;
        Vector2 tip = basePoint + tangent * thickness * 1.6f;
        float Shape(Vector2 p) => Mathf.Min(Arc(p, c, radius, thickness, start, sweep), Triangle(p, tipA, tip, tipB) - 1.5f);
        Paint(px, w, h, Shape, p => Color.Lerp(new Color32(0x2B, 0x86, 0xE0, 0xFF), new Color32(0x74, 0xCC, 0xFF, 0xFF), Mathf.InverseLerp(c.y - radius, c.y + radius, p.y)),
            new Color32(0x1A, 0x55, 0x9C, 0xFF), 3.5f);
        // A soft highlight along the top of the ring.
        Paint(px, w, h, p => Arc(p, c + new Vector2(0f, 3f), radius + 1f, 4f, 165f, 120f), _ => new Color(1f, 1f, 1f, 0.45f), Color.clear, 0f);
        SaveSprite(px, w, h, RotateSpritePath);
    }

    // The Daily Reward tile with a paw-print calendar; the label is a Text child in the scene.
    private static void DrawDailyPuzzleButton()
    {
        Color[] px = LoadPixels(DailyRewardSourcePath, out int w, out int h);
        Color face = BlankFace(px, w, h);
        Vector2 c = new Vector2(w * 0.5f, 92f);
        Color Brown(Vector2 _) => LabelBrown;
        // Binder pegs, the calendar body, its page, then a paw on the page.
        Paint(px, w, h, p => Mathf.Min(Box(p, c + new Vector2(-14f, 30f), new Vector2(3.5f, 8f), 3.5f), Box(p, c + new Vector2(14f, 30f), new Vector2(3.5f, 8f), 3.5f)), Brown, Color.clear, 0f);
        Paint(px, w, h, p => Box(p, c, new Vector2(31f, 27f), 8f), Brown, Color.clear, 0f);
        Paint(px, w, h, p => Box(p, c + new Vector2(0f, -7f), new Vector2(24f, 15f), 4f), _ => face, Color.clear, 0f);
        Vector2 paw = c + new Vector2(0f, -9f);
        Paint(px, w, h, p => Mathf.Min(Ellipse(p, paw + new Vector2(0f, -3f), new Vector2(7.5f, 6f)),
            Mathf.Min(Mathf.Min(Circle(p, paw + new Vector2(-9f, 4f), 3.2f), Circle(p, paw + new Vector2(-3.5f, 8.5f), 3.2f)),
                Mathf.Min(Circle(p, paw + new Vector2(3.5f, 8.5f), 3.2f), Circle(p, paw + new Vector2(9f, 4f), 3.2f)))), Brown, Color.clear, 0f);
        SaveSprite(px, w, h, DailyPuzzleSpritePath);
    }

    // A green check badge for a won Daily Puzzle.
    private static void DrawDoneMark()
    {
        const int size = 72;
        var px = new Color[size * size];
        var c = new Vector2(size * 0.5f, size * 0.5f);
        Paint(px, size, size, p => Circle(p, c, 30f),
            p => Color.Lerp(new Color32(0x27, 0xA8, 0x45, 0xFF), new Color32(0x5B, 0xDD, 0x6E, 0xFF), Mathf.InverseLerp(c.y - 30f, c.y + 30f, p.y)),
            Color.white, 4f);
        Paint(px, size, size, p => Mathf.Min(Segment(p, new Vector2(22f, 37f), new Vector2(31f, 27f)), Segment(p, new Vector2(31f, 27f), new Vector2(50f, 47f))) - 4.5f,
            _ => Color.white, Color.clear, 0f);
        SaveSprite(px, size, size, DoneMarkSpritePath);
    }

    private static Color[] LoadPixels(string path, out int width, out int height)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        width = texture.width;
        height = texture.height;
        Color[] pixels = texture.GetPixels();
        UnityEngine.Object.DestroyImmediate(texture);
        return pixels;
    }

    /// <summary>
    /// Wipes the icon and label off a cream button face: each row is refilled with a blend of the
    /// cream found just inside the rim on either side. Returns the face colour at the centre.
    /// </summary>
    private static Color BlankFace(Color[] px, int w, int h)
    {
        var filled = new bool[px.Length];
        for (int y = 0; y < h; y++)
        {
            int left = -1, right = -1;
            for (int x = 0; x < w; x++)
            {
                if (!IsCream(px[y * w + x])) continue;
                if (left < 0) left = x;
                right = x;
            }
            if (left < 0 || right - left < w / 3) continue;
            int l = left + 4, r = right - 4;
            Color cl = (px[y * w + l - 1] + px[y * w + l] + px[y * w + l + 1]) / 3f;
            Color cr = (px[y * w + r - 1] + px[y * w + r] + px[y * w + r + 1]) / 3f;
            for (int x = l + 1; x < r; x++)
            {
                px[y * w + x] = Color.Lerp(cl, cr, (x - l) / (float)(r - l));
                filled[y * w + x] = true;
            }
        }

        // Rows were filled one by one; blend them vertically so no streaks show.
        var source = (Color[])px.Clone();
        const int reach = 6;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!filled[y * w + x]) continue;
            Color sum = Color.clear;
            int count = 0;
            for (int dy = -reach; dy <= reach; dy++)
            {
                int yy = y + dy;
                if (yy < 0 || yy >= h || !filled[yy * w + x]) continue;
                sum += source[yy * w + x];
                count++;
            }
            px[y * w + x] = sum / count;
        }
        return px[(h / 2) * w + w / 2];
    }

    private static bool IsCream(Color c) => c.a > 0.95f && c.r > 0.9f && c.g > 0.84f && c.b > 0.72f && c.r - c.b < 0.2f;

    /// <summary>Alpha-blends a signed-distance shape (negative inside) with an optional outline ring.</summary>
    private static void Paint(Color[] px, int w, int h, Func<Vector2, float> distance, Func<Vector2, Color> fill, Color outline, float outlineWidth)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float d = distance(p);
            float coverage = Mathf.Clamp01(outlineWidth + 0.5f - d);
            if (coverage <= 0f) continue;
            Color inner = fill(p);
            Color colour = outlineWidth > 0f ? Color.Lerp(outline, inner, Mathf.Clamp01(0.5f - d)) : inner;
            float alpha = colour.a * coverage;
            Color under = px[y * w + x];
            float outA = alpha + under.a * (1f - alpha);
            Color blended = outA > 0f ? (colour * alpha + under * under.a * (1f - alpha)) / outA : Color.clear;
            blended.a = outA;
            px[y * w + x] = blended;
        }
    }

    private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    private static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
    {
        Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
        return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
    }

    private static float Box(Vector2 p, Vector2 c, Vector2 half, float corner)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(corner, corner);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner;
    }

    private static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    /// <summary>A ring band swept clockwise from <paramref name="startDegrees"/>, with round ends.</summary>
    private static float Arc(Vector2 p, Vector2 c, float radius, float thickness, float startDegrees, float sweepDegrees)
    {
        Vector2 v = p - c;
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        float travelled = Mathf.Repeat(startDegrees - angle, 360f);
        if (travelled <= sweepDegrees) return Mathf.Abs(v.magnitude - radius) - thickness * 0.5f;
        float a0 = startDegrees * Mathf.Deg2Rad, a1 = (startDegrees - sweepDegrees) * Mathf.Deg2Rad;
        Vector2 e0 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
        Vector2 e1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
        return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - thickness * 0.5f;
    }

    private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
        Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
        Vector2 q0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / e0.sqrMagnitude);
        Vector2 q1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / e1.sqrMagnitude);
        Vector2 q2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / e2.sqrMagnitude);
        float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
        Vector2 d = Vector2.Min(Vector2.Min(new Vector2(q0.sqrMagnitude, s * (v0.x * e0.y - v0.y * e0.x)),
            new Vector2(q1.sqrMagnitude, s * (v1.x * e1.y - v1.y * e1.x))), new Vector2(q2.sqrMagnitude, s * (v2.x * e2.y - v2.y * e2.x)));
        return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
    }

    private static void SaveSprite(Color[] px, int w, int h, string path)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.SetPixels(px);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
