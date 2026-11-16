using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The project's control panel (Cat Block Puzzle > Setup), the one place the game is set up from: a title bar, a status
/// line for the core assets, then the areas grouped under headings (gameplay, levels, look, audio, tools), each a
/// collapsible box with a "?" that explains it. Every box edits the real assets or the GameScene components, or runs a
/// tool, so nothing lives only in this window.
/// </summary>
public sealed class CatPuzzleSetupWindow : EditorWindow
{
    private const string ScenePath = "Assets/_GameData/System/Scenes/GameScene.unity";
    private const string LoadingScenePath = "Assets/_GameData/System/Scenes/Loading.unity";
    private const string ContentPath = "Assets/_GameData/Art/UI/CatPuzzleScene/Content.asset";
    private const string VisualsPath = "Assets/_GameData/Art/UI/CatPuzzleScene/Visuals.asset";
    private const string ResourcesFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle";
    private const string ShopPath = ResourcesFolder + "/PowerUpShopConfig.asset";
    private const string DailyPath = ResourcesFolder + "/DailyRewardConfig.asset";
    private const string ThemePath = ResourcesFolder + "/GameUiTheme.asset";
    private const string LayoutPath = ResourcesFolder + "/portrait_layout_profile.asset";
    private const string LevelsJsonPath = ResourcesFolder + "/levels_100.json";
    private const string ApkPath = "Builds/CatBlockPuzzle.apk";

    private const string Prefs = "CatPuzzleSetup.";
    private const string CoinsKey = "CatBlockPuzzle.Coins";
    private const string LevelKey = "CatBlockPuzzle.LevelIndex";
    private static readonly string[] TutorialKeys =
    {
        "CatBlockPuzzle.Tutorial.Hint", "CatBlockPuzzle.Tutorial.Freeze", "CatBlockPuzzle.Tutorial.Rotate",
        "CatBlockPuzzle.Tutorial.Hammer", "CatBlockPuzzle.Tutorial.Timer",
    };

    private Vector2 scroll;
    private readonly Dictionary<string, bool> helpOpen = new Dictionary<string, bool>();
    private readonly Dictionary<UnityEngine.Object, Editor> editors = new Dictionary<UnityEngine.Object, Editor>();
    private readonly Dictionary<UnityEngine.Object, SerializedObject> sceneObjects = new Dictionary<UnityEngine.Object, SerializedObject>();
    private readonly List<string> sectionKeys = new List<string>();
    private GUIStyle titleStyle, subtitleStyle, sectionTitle, categoryStyle, bodyStyle, helpButton, statusStyle;
    private Texture2D panelTex;
    private int jumpLevel = 1, startLevel = 1, testCoins = 5000, auditFrom = 1, auditTo = 300;
    private string lastReport;
    private readonly CatPuzzleLevelPreview levelPreview = new CatPuzzleLevelPreview();

    [MenuItem("Cat Block Puzzle/Setup", priority = 1)]
    public static void Open()
    {
        var w = GetWindow<CatPuzzleSetupWindow>();
        w.titleContent = new GUIContent("Cat Block Puzzle", EditorGUIUtility.IconContent("SettingsIcon").image);
        w.minSize = new Vector2(420f, 360f);
        w.Show();
    }

    private void OnDisable()
    {
        foreach (var e in editors.Values) if (e != null) DestroyImmediate(e);
        editors.Clear();
        sceneObjects.Clear();
        if (panelTex != null) DestroyImmediate(panelTex);
    }

    private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

    // ---------- window theme (stored per user in EditorPrefs, not in the game) ----------
    private sealed class Theme { public Color Background, Header, Accent, Section, Panel, Text; }

    private static readonly (string name, string bg, string header, string accent, string section, string panel, string text)[] Presets =
    {
        ("Cozy", "2A2420", "4A3020", "FFB347", "5A3C28", "322B26", "FFF4E2"),
        ("Mint", "1D2A27", "15463C", "5BE3B0", "21584C", "24332F", "E8FFF7"),
        ("Sky", "1E2633", "16304F", "4FC3F7", "24405E", "26303D", "EAF4FF"),
        ("Berry", "271F2D", "45225A", "F06CB4", "55306D", "30273A", "FBEAFF"),
        ("Classic", "383838", "232323", "B6F06A", "2E2E2E", "3F3F3F", "FFFFFF"),
    };

    private const string ThemeKey = Prefs + "theme.";
    private static readonly string[] ThemeParts = { "Background", "Header", "Accent", "Section", "Panel", "Text" };
    private Theme theme;

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.gray;

    private static Theme LoadTheme()
    {
        var p = Presets[0];
        string[] defaults = { p.bg, p.header, p.accent, p.section, p.panel, p.text };
        var c = new Color[6];
        for (int i = 0; i < 6; i++) c[i] = Hex(EditorPrefs.GetString(ThemeKey + ThemeParts[i], defaults[i]));
        return new Theme { Background = c[0], Header = c[1], Accent = c[2], Section = c[3], Panel = c[4], Text = c[5] };
    }

    private void SaveTheme()
    {
        Color[] c = { theme.Background, theme.Header, theme.Accent, theme.Section, theme.Panel, theme.Text };
        for (int i = 0; i < 6; i++) EditorPrefs.SetString(ThemeKey + ThemeParts[i], ColorUtility.ToHtmlStringRGB(c[i]));
        titleStyle = null;
    }

    private void ApplyPreset(int i)
    {
        var p = Presets[i];
        theme = new Theme { Background = Hex(p.bg), Header = Hex(p.header), Accent = Hex(p.accent), Section = Hex(p.section), Panel = Hex(p.panel), Text = Hex(p.text) };
        SaveTheme();
    }

    private void Styles()
    {
        if (theme == null) theme = LoadTheme();
        if (titleStyle != null && panelTex != null) return;
        titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 22 };
        titleStyle.normal.textColor = theme.Text;
        subtitleStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
        subtitleStyle.normal.textColor = new Color(theme.Text.r, theme.Text.g, theme.Text.b, 0.65f);
        sectionTitle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 13 };
        sectionTitle.normal.textColor = theme.Text;
        categoryStyle = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 10, padding = new RectOffset(10, 0, 8, 2) };
        categoryStyle.normal.textColor = theme.Accent;
        statusStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
        if (panelTex == null) panelTex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        panelTex.SetPixel(0, 0, theme.Panel); panelTex.Apply();
        bodyStyle = new GUIStyle { padding = new RectOffset(12, 10, 8, 10), margin = new RectOffset(6, 6, 0, 4) };
        bodyStyle.normal.background = panelTex;
        helpButton = new GUIStyle(EditorStyles.miniButton) { fixedWidth = 22, fixedHeight = 18, fontStyle = FontStyle.Bold };
    }

    // ---------- layout ----------
    private void OnGUI()
    {
        Styles();
        EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), theme.Background);
        DrawTitle();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        sectionKeys.Clear();

        EditorGUILayout.BeginVertical(bodyStyle);
        DrawStatus();
        EditorGUILayout.EndVertical();

        GUILayout.Label("GAMEPLAY", categoryStyle);
        DrawPowerUps();
        DrawTimerAndDifficulty();
        DrawPopups();
        DrawAssetSection("shop", "Shop & Prices", ShopPath, "What the shop sells: each power-up's coin price and pack size, the Starter Bundle and the coin packs (amount and store price label).");
        DrawAssetSection("daily", "Daily Rewards", DailyPath, "The seven-day login calendar: what each day pays (coins, Hints, Freezes, Hammers) and the day 7 special.");
        DrawDailyPuzzle();

        GUILayout.Label("LEVELS & TUTORIALS", categoryStyle);
        DrawLevels();
        DrawTutorials();

        GUILayout.Label("LOOK & SCREENS", categoryStyle);
        DrawAssetSection("theme.ui", "UI Theme", ThemePath, "The game's UI colours and fonts.");
        DrawAssetSection("layout", "Portrait Layout", LayoutPath, "Where the board and the HUD sit on the 1080x1920 frame. Every page is that frame scaled into the phone's safe area, so it fits every phone and tablet.", "UI Tuning window", () => EditorApplication.ExecuteMenuItem("Cat Block Puzzle/UI Tuning"));
        DrawAssetSection("visuals", "Cat Visuals", VisualsPath, "The cat pictures and colours the board uses for each shape.");

        GUILayout.Label("AUDIO", categoryStyle);
        DrawAudio();

        GUILayout.Label("TOOLS", categoryStyle);
        DrawTesting();
        DrawPaths();
        DrawBuild();

        GUILayout.Label("WINDOW", categoryStyle);
        DrawThemeSection();

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTitle()
    {
        var r = GUILayoutUtility.GetRect(position.width, 64f);
        EditorGUI.DrawRect(r, theme.Header);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 3f, r.width, 3f), theme.Accent);
        GUI.Label(new Rect(r.x + 16f, r.y + 8f, r.width - 200f, 30f), PlayerSettings.productName, titleStyle);
        GUI.Label(new Rect(r.x + 16f, r.y + 38f, r.width - 200f, 18f), "Game setup  ·  every value here is saved in the game's assets or GameScene", subtitleStyle);
        if (GUI.Button(new Rect(r.xMax - 176f, r.y + 20f, 80f, 22f), "Expand all")) foreach (var k in sectionKeys) EditorPrefs.SetBool(Prefs + k, true);
        if (GUI.Button(new Rect(r.xMax - 90f, r.y + 20f, 80f, 22f), "Collapse all")) foreach (var k in sectionKeys) EditorPrefs.SetBool(Prefs + k, false);
    }

    private void DrawStatus()
    {
        string Dot(bool ok, string what) => ok ? "<color=#7BE38F>●</color> " + what : "<color=#FF6B6B>● " + what + " missing</color>";
        bool sceneOpen = SceneObject("GameSystem") != null;
        int levels = CountLevels();
        EditorGUILayout.LabelField(string.Join("    ", new[]
        {
            Dot(levels > 0, levels + " levels"),
            Dot(Load(ShopPath) != null, "shop"),
            Dot(Load(DailyPath) != null, "daily rewards"),
            Dot(Load(ThemePath) != null, "UI theme"),
            Dot(Load(VisualsPath) != null, "cat visuals"),
            sceneOpen ? Dot(true, "GameScene open") : "<color=#FFC857>● GameScene not open (scene settings hidden)</color>",
        }), statusStyle);
        EditorGUILayout.BeginHorizontal();
        if (!sceneOpen && !EditorApplication.isPlaying && GUILayout.Button("Open GameScene", GUILayout.Width(130)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
        var active = EditorSceneManager.GetActiveScene();
        if (sceneOpen && active.isDirty && !EditorApplication.isPlaying && GUILayout.Button("Save GameScene", GUILayout.Width(130))) EditorSceneManager.SaveScene(active);
        EditorGUILayout.EndHorizontal();
        if (EditorApplication.isPlaying)
            EditorGUILayout.LabelField($"<color=#{ColorUtility.ToHtmlStringRGB(theme.Accent)}>Playing</color>   saved level {PlayerPrefs.GetInt(LevelKey, 0) + 1}   ·   {Coins()} coins", statusStyle);
    }

    // ---------- gameplay ----------
    private void DrawPowerUps()
    {
        if (!Section("powerups", "Power-ups", "The level each power-up unlocks on (its spotlight tutorial shows there), how many free ones a player gets when it unlocks, and how long Freeze stops the clock. The Hammer unlocks on the first level with a padlock.")) return;
        if (!SceneFields("GameplayHudView", ("hintUnlockLevel", "Hint unlocks on level"), ("freezeUnlockLevel", "Freeze unlocks on level"),
                ("hammerUnlockLevel", "Hammer unlocks on level"), ("rotateUnlockLevel", "Rotate unlocks on level"), ("freeOnUnlock", "Free on unlock")))
            NeedScene();
        SceneFields("CatBlockPuzzleGame", ("freezeDurationSeconds", "Freeze seconds"));
        EndSection();
    }

    private void DrawTimerAndDifficulty()
    {
        if (!Section("timer", "Timer & Difficulty", "The level the countdown timer starts on (the timer tutorial plays there; earlier levels have no timer and always earn 3 stars), whether the big EASY / MEDIUM / HARD banner shows when the difficulty changes, and which 1-10 level ratings count as Medium and Hard. Each level's rating itself is set in the level pack (levels_100.json).")) return;
        if (!SceneFields("GameSystem", ("timerStartLevel", "Timer starts on level"), ("showDifficultyBanner", "Show difficulty banner"),
                ("mediumFromRating", "Medium from rating"), ("hardFromRating", "Hard from rating")))
            NeedScene();
        EndSection();
    }

    private void DrawPopups()
    {
        if (!Section("popups", "Popups", "One dim is shared by every popup (the daily puzzle, locked power-up and new-mechanic cards, and the Out of Hints purchase popup): a single object that turns on behind whichever popup opens and fades in and out. This sets how dark it is, from 0 (clear) to 255 (black).")) return;
        if (!SceneFields("GameSystem", ("popupDimAlpha", "Dim darkness (0-255)")))
            NeedScene();
        EndSection();
    }

    private void DrawDailyPuzzle()
    {
        if (!Section("dailypuzzle", "Daily Puzzle", "When the Daily Puzzle button opens and what the day's first win pays: coins for each star, plus a streak bonus at 3 days (a Hint), every 7 days (a Freeze and a Hammer) and every 30 days (500 coins).")) return;
        if (!SceneFields("GameSystem", ("dailyPuzzleUnlockAfterLevel", "Opens after level"), ("dailyPuzzleCoinReward", "Coins per star")))
            NeedScene();
        EndSection();
    }

    // ---------- levels and tutorials ----------
    private void DrawLevels()
    {
        if (!Section("levels", "Levels", "The level pack (levels_100.json, copied into Content.asset; the two must match) and the level prefabs baked from it. The preview draws any level like the game: the board, each cat on its spot, stones, padlocks (with the number of cats that opens them), fog and colour zones, the cats in the tray, and a strip of the levels around it. After changing levels: Refresh Level Data, then Rebake Level Prefabs if a grid size or piece count changed. Validate checks the scene. The Hint audit follows the Hint through every level, also after random wrong drops, and reports any level where it points somewhere unusable.")) return;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Level pack (JSON)", Load(LevelsJsonPath), typeof(TextAsset), false);
            EditorGUILayout.ObjectField("Content", Load(ContentPath), typeof(ScriptableObject), false);
        }
        EditorGUILayout.LabelField(CountLevels() + " levels", EditorStyles.miniLabel);
        ToolRow(("Refresh Level Data", "Reads levels_100.json into Content.asset.", () => EditorApplication.ExecuteMenuItem("Cat Block Puzzle/Refresh Level Data")),
                ("Rebake Level Prefabs", "Rebuilds the level prefabs on a copy of the scene; GameScene's UI stays as it is.", () => EditorApplication.ExecuteMenuItem("Cat Block Puzzle/Rebake Level Prefabs (Keep Scene)")),
                ("Validate Game Scene", "Checks that GameScene is wired up correctly.", () => EditorApplication.ExecuteMenuItem("Cat Block Puzzle/Validate Game Scene")));
        Sub("Hint audit");
        EditorGUILayout.BeginHorizontal();
        auditFrom = Mathf.Max(1, EditorGUILayout.IntField("Levels", auditFrom));
        auditTo = Mathf.Max(auditFrom, EditorGUILayout.IntField(auditTo, GUILayout.Width(60)));
        if (GUILayout.Button("Run", GUILayout.Width(60)))
            EditorApplication.delayCall += () => { lastReport = RunHintAudit(auditFrom - 1, auditTo - 1); Debug.Log(lastReport); };
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(lastReport)) EditorGUILayout.HelpBox(lastReport.Length > 600 ? lastReport.Substring(0, 600) + "…" : lastReport, MessageType.None);
        Sub("Level preview");
        levelPreview.Draw(theme.Accent);
        EndSection();
    }

    // HintAudit is internal to the game assembly, so it is reached by name.
    private static string RunHintAudit(int from, int to)
    {
        var type = typeof(CatBlockPuzzle.GameSystem).Assembly.GetType("CatBlockPuzzle.HintAudit");
        var run = type?.GetMethod("Run", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        return run == null ? "Hint audit not found." : (string)run.Invoke(null, new object[] { from, to, 0, 0 });
    }

    private void DrawTutorials()
    {
        if (!Section("tutorials", "Tutorials", "The hands-on level 1 tutorial (on or off, captions, the hand picture) and the spotlight tutorials that introduce the timer and each power-up the first time they are reached.")) return;
        if (!SceneFields("CatBlockPuzzleGame", ("enableLevelOneTutorial", "Level 1 tutorial"), ("showTutorialCaptions", "Captions"), ("tutorialHandSprite", "Hand picture")))
            NeedScene();
        ToolRow(("Show power-up tutorials again", "Forgets which power-up and timer tutorials were seen, so each shows again on its level.", () =>
        {
            foreach (var k in TutorialKeys) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }));
        EndSection();
    }

    // ---------- audio ----------
    private void DrawAudio()
    {
        if (!Section("audio", "Sound", "Every sound the game plays. Sound Test plays each one so you can check the clips and volumes.")) return;
        ToolRow(("Sound Test", "Opens the sound tester.", () => EditorApplication.ExecuteMenuItem("Cat Block Puzzle/Audio/Sound Test")));
        EndSection();
    }

    // ---------- tools ----------
    private void DrawTesting()
    {
        if (!Section("testing", "Testing", "Test switches in GameScene (turn them off for release), then: play any level, set the level the game opens on, give coins, reset progress, and the random-play bot.")) return;
        SceneFields("GameSystem", ("unlockAllLevels", "Unlock all levels"), ("showLevelTestButtons", "Level test buttons"));

        Sub("Levels");
        EditorGUILayout.BeginHorizontal();
        jumpLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Play level", "Opens this level now. Needs play mode."), jumpLevel));
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            if (GUILayout.Button("Play", GUILayout.Width(70)) && CatBlockPuzzle.GameSystem.Instance != null) CatBlockPuzzle.GameSystem.Instance.PreviewLevel(jumpLevel - 1);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        startLevel = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Start on level", "The saved level the game opens on next start (now " + (PlayerPrefs.GetInt(LevelKey, 0) + 1) + ")."), startLevel));
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            if (GUILayout.Button("Set", GUILayout.Width(70))) { PlayerPrefs.SetInt(LevelKey, startLevel - 1); PlayerPrefs.Save(); }
        EditorGUILayout.EndHorizontal();

        Sub("Coins");
        EditorGUILayout.BeginHorizontal();
        testCoins = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Coins", "Now " + Coins() + "."), testCoins));
        if (GUILayout.Button("Add", GUILayout.Width(60))) AddCoins(testCoins);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            if (GUILayout.Button("Set", GUILayout.Width(60))) { PlayerPrefs.SetInt(CoinsKey, testCoins); PlayerPrefs.Save(); }
        EditorGUILayout.EndHorizontal();

        Sub("Bots and progress");
        ToolRow(("Run random-play bot", "Plays levels with random taps to catch crashes and stuck states. Needs play mode.", () =>
                {
                    if (!EditorApplication.isPlaying) EditorUtility.DisplayDialog("Random-play bot", "Enter play mode first.", "OK");
                    else EditorApplication.ExecuteMenuItem("Cat Block Puzzle/Testing/Run Dumb Player Bot (Play Mode)");
                }),
                ("Reset saved progress", "Deletes every saved value for this project: level, stars, coins, power-ups, tutorials, settings.", () =>
                {
                    if (EditorUtility.DisplayDialog("Reset saved progress", "This deletes all saved progress and settings for this project. Continue?", "Reset", "Cancel")) { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }
                }));
        EndSection();
    }

    private void DrawPaths()
    {
        if (!Section("paths", "Asset Paths", "Where the main assets live. Read only; click one to select it.")) return;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Loading scene", Load(LoadingScenePath), typeof(SceneAsset), false);
            EditorGUILayout.ObjectField("Game scene", Load(ScenePath), typeof(SceneAsset), false);
            EditorGUILayout.ObjectField("Level pack", Load(LevelsJsonPath), typeof(TextAsset), false);
            EditorGUILayout.ObjectField("Content", Load(ContentPath), typeof(ScriptableObject), false);
            EditorGUILayout.ObjectField("Cat visuals", Load(VisualsPath), typeof(ScriptableObject), false);
            EditorGUILayout.ObjectField("Shop", Load(ShopPath), typeof(ScriptableObject), false);
            EditorGUILayout.ObjectField("Daily rewards", Load(DailyPath), typeof(ScriptableObject), false);
            EditorGUILayout.ObjectField("UI theme", Load(ThemePath), typeof(ScriptableObject), false);
        }
        EndSection();
    }

    private void DrawBuild()
    {
        if (!Section("build", "Build", "Builds the Android APK to " + ApkPath + " with the scenes in Build Settings. The Build Report Tool then has the size breakdown (Window > Build Report).")) return;
        EditorGUILayout.LabelField("Bundle id", PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android), EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Version", PlayerSettings.bundleVersion + "  (code " + PlayerSettings.Android.bundleVersionCode + ")", EditorStyles.miniLabel);
        if (File.Exists(ApkPath)) EditorGUILayout.LabelField("Last APK", $"{new FileInfo(ApkPath).Length / 1048576f:0.0} MB  ·  {File.GetLastWriteTime(ApkPath):d MMM HH:mm}", EditorStyles.miniLabel);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            ToolRow(("Build Android APK", "Builds " + ApkPath + ". Takes a few minutes.", BuildApk),
                    ("Show APK in Finder", "Reveals the last build.", () => { if (File.Exists(ApkPath)) EditorUtility.RevealInFinder(ApkPath); }));
        EndSection();
    }

    private static void BuildApk()
    {
        if (!EditorUtility.DisplayDialog("Build Android APK", "Build " + ApkPath + " now? It takes a few minutes.", "Build", "Cancel")) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
        });
        Debug.Log($"APK build {report.summary.result}: {report.summary.totalSize / 1048576f:0.0} MB, {report.summary.totalErrors} errors.");
    }

    // ---------- window colours ----------
    private void DrawThemeSection()
    {
        if (!Section("windowtheme", "Window Colours", "The colours of this window only (saved for you on this computer, not in the game). Pick a preset or set each colour.")) return;
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < Presets.Length; i++)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = Hex(Presets[i].accent);
            if (GUILayout.Button(Presets[i].name)) ApplyPreset(i);
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        theme.Background = EditorGUILayout.ColorField("Background", theme.Background);
        theme.Header = EditorGUILayout.ColorField("Title Bar", theme.Header);
        theme.Accent = EditorGUILayout.ColorField("Accent", theme.Accent);
        theme.Section = EditorGUILayout.ColorField("Section Header", theme.Section);
        theme.Panel = EditorGUILayout.ColorField("Section Panel", theme.Panel);
        theme.Text = EditorGUILayout.ColorField("Header Text", theme.Text);
        if (EditorGUI.EndChangeCheck()) SaveTheme();
        EndSection();
    }

    // ---------- building blocks ----------
    private bool Section(string key, string title, string help)
    {
        sectionKeys.Add(key);
        var head = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
        head.x += 6f; head.width -= 12f;
        bool open = EditorPrefs.GetBool(Prefs + key, false);
        EditorGUI.DrawRect(head, theme.Section);
        EditorGUI.DrawRect(new Rect(head.x, head.y, 4f, head.height), theme.Accent);
        GUI.Label(new Rect(head.x + 12f, head.y, 16f, head.height), open ? "▾" : "▸", sectionTitle);
        GUI.Label(new Rect(head.x + 30f, head.y, head.width - 70f, head.height), title, sectionTitle);
        helpOpen.TryGetValue(key, out bool showHelp);
        if (GUI.Button(new Rect(head.xMax - 30f, head.y + 6f, 22f, 18f), "?", helpButton)) helpOpen[key] = showHelp = !showHelp;
        else if (Event.current.type == EventType.MouseDown && head.Contains(Event.current.mousePosition))
        {
            open = !open;
            EditorPrefs.SetBool(Prefs + key, open);
            Event.current.Use();
        }
        if (showHelp) { EditorGUILayout.BeginHorizontal(); GUILayout.Space(6f); EditorGUILayout.HelpBox(help, MessageType.Info); GUILayout.Space(6f); EditorGUILayout.EndHorizontal(); }
        if (!open) { GUILayout.Space(3f); return false; }
        EditorGUILayout.BeginVertical(bodyStyle);
        return true;
    }

    private static void EndSection() { EditorGUILayout.EndVertical(); GUILayout.Space(3f); }

    private static void Sub(string title) { EditorGUILayout.Space(4); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }

    private static void NeedScene() => EditorGUILayout.HelpBox("Open GameScene to edit these (they are stored on its components).", MessageType.None);

    /// <summary>A whole asset drawn with its own inspector, plus an optional tool button.</summary>
    private void DrawAssetSection(string key, string title, string path, string help, string toolLabel = null, Action tool = null)
    {
        if (!Section(key, title, help)) return;
        var asset = Load(path);
        if (asset == null) EditorGUILayout.HelpBox(path + " is missing.", MessageType.Warning);
        else
        {
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Asset", asset, typeof(UnityEngine.Object), false);
            editors.TryGetValue(asset, out var ed);
            Editor.CreateCachedEditor(asset, null, ref ed);
            editors[asset] = ed;
            ed.OnInspectorGUI();
        }
        if (toolLabel != null) ToolRow((toolLabel, "", tool));
        EndSection();
    }

    /// <summary>Draws named serialized fields of a GameScene component. False when the component is not in an open scene.</summary>
    private bool SceneFields(string typeName, params (string field, string label)[] fields)
    {
        var target = SceneObject(typeName);
        if (target == null) return false;
        if (!sceneObjects.TryGetValue(target, out var so) || so.targetObject == null) sceneObjects[target] = so = new SerializedObject(target);
        so.Update();
        foreach (var (field, label) in fields)
        {
            var p = so.FindProperty(field);
            if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label, p.tooltip), true);
        }
        so.ApplyModifiedProperties();
        return true;
    }

    private static UnityEngine.Object SceneObject(string typeName)
    {
        var type = typeof(CatBlockPuzzle.GameSystem).Assembly.GetType("CatBlockPuzzle." + typeName);
        return type == null ? null : UnityEngine.Object.FindFirstObjectByType(type, FindObjectsInactive.Include);
    }

    private static UnityEngine.Object Load(string path) => AssetDatabase.LoadMainAssetAtPath(path);

    private static int CountLevels()
    {
        var json = Load(LevelsJsonPath) as TextAsset;
        if (json == null) return 0;
        int n = 0, i = 0;
        while ((i = json.text.IndexOf("\"level_", i, StringComparison.Ordinal)) >= 0) { n++; i++; }
        return n;
    }

    private static int Coins() =>
        EditorApplication.isPlaying && CatBlockPuzzle.GameSystem.Instance != null ? CatBlockPuzzle.GameSystem.Instance.CurrentCoins : PlayerPrefs.GetInt(CoinsKey, 0);

    private static void AddCoins(int amount)
    {
        if (EditorApplication.isPlaying && CatBlockPuzzle.GameSystem.Instance != null) CatBlockPuzzle.GameSystem.Instance.GrantShopCoins(amount);
        else { PlayerPrefs.SetInt(CoinsKey, PlayerPrefs.GetInt(CoinsKey, 0) + amount); PlayerPrefs.Save(); }
    }

    private static void ToolRow(params (string label, string tip, Action run)[] buttons)
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var (label, tip, run) in buttons)
            if (GUILayout.Button(new GUIContent(label, tip), GUILayout.Height(24))) EditorApplication.delayCall += () => run();
        EditorGUILayout.EndHorizontal();
    }
}
