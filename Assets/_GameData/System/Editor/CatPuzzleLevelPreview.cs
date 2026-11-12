using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The level preview in Cat Block Puzzle > Setup > Levels: a level drawn like the game (cream board, a coloured cat on
/// each piece's spot, stones, padlocks, fog and colour zones), with level navigation, a strip of the levels around it,
/// its facts and the cats in the tray. Reads levels_100.json, the same pack the game plays.
/// </summary>
internal sealed class CatPuzzleLevelPreview
{
    private const string LevelsJsonPath = "Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json";

    [Serializable] private sealed class Pack { public LevelJson[] levels; }
    [Serializable] private sealed class LevelJson
    {
        public string id, title;
        public int difficulty, rows, cols, reward, trayWindow, moves;
        public PieceJson[] pieces, spares;
        public CellJson[] blocked, locked, zones, fog;
    }
    [Serializable] private sealed class PieceJson { public string id, name, shape; public int row, col; public bool @fixed; }
    [Serializable] private sealed class CellJson { public int row, col, after; public string piece; }

    public int Level = 1;
    private bool showCats = true;
    private Pack pack;
    private DateTime packTime;
    private GUIStyle chip, small, heading, cellText;

    private static readonly Color Card = Hex("FBF1DD"), CardEdge = Hex("E2CFA6"), Well = Hex("EADBBE"), Cell = Hex("FFF8E8"), CellEdge = Hex("E5D0A8");
    private static readonly Color Stone = Hex("8C8580"), LockColor = Hex("C9A227"), Ink = Hex("4A2E2A");
    private static readonly Color[] CatColors =
    {
        Hex("F6BE3E"), Hex("FF8FA6"), Hex("B58CF0"), Hex("66D0B7"), Hex("6FA7F5"), Hex("8BD35B"), Hex("FFA05C"), Hex("F07A7A"), Hex("5CC8E0"), Hex("D49A6A"),
    };

    private static Color Hex(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.magenta;

    public void Draw(Color accent)
    {
        if (!LoadPack()) { EditorGUILayout.HelpBox("Could not read " + LevelsJsonPath + ".", MessageType.Warning); return; }
        int count = pack.levels.Length;
        Level = Mathf.Clamp(Level, 1, count);
        Styles();
        var level = pack.levels[Level - 1];

        EditorGUILayout.Space(4);
        DrawNav(count, level);
        DrawStrip(count, accent);
        EditorGUILayout.Space(6);

        // facts
        int pieceCount = level.pieces != null ? level.pieces.Length : 0;
        EditorGUILayout.BeginHorizontal();
        Chip(level.rows + " x " + level.cols, accent);
        Chip(pieceCount + " cats", accent);
        Chip("difficulty " + level.difficulty, accent);
        Chip(level.reward + " coins", accent);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (Len(level.blocked) > 0) Chip(Len(level.blocked) + " stone" + (Len(level.blocked) == 1 ? "" : "s"), Stone);
        if (Len(level.locked) > 0) Chip(Len(level.locked) + " padlock" + (Len(level.locked) == 1 ? "" : "s"), LockColor);
        if (Len(level.fog) > 0) Chip(Len(level.fog) + " fog", Hex("9DB4C8"));
        if (Len(level.zones) > 0) Chip(Len(level.zones) + " zoned", Hex("B58CF0"));
        if (Len(level.spares) > 0) Chip(Len(level.spares) + " spare cat" + (Len(level.spares) == 1 ? "" : "s"), Hex("F07A7A"));
        if (level.trayWindow > 0) Chip("tray shows " + level.trayWindow, accent);
        if (level.moves > 0) Chip(level.moves + " moves", accent);
        EditorGUILayout.EndHorizontal();

        showCats = EditorGUILayout.ToggleLeft("Show the cats on their spots (the solution)", showCats);
        DrawBoard(level, Mathf.Min(EditorGUIUtility.currentViewWidth - 60f, 420f));
        DrawTray(level);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            if (GUILayout.Button(new GUIContent("Play this level", "Opens it in the game. Needs play mode."), GUILayout.Height(24)) && CatBlockPuzzle.GameSystem.Instance != null)
                CatBlockPuzzle.GameSystem.Instance.PreviewLevel(Level - 1);
    }

    private static int Len<T>(T[] a) => a == null ? 0 : a.Length;

    private bool LoadPack()
    {
        // read once, and again only when the file changes
        if (!System.IO.File.Exists(LevelsJsonPath)) return false;
        var time = System.IO.File.GetLastWriteTimeUtc(LevelsJsonPath);
        if (pack == null || time != packTime)
        {
            try { pack = JsonUtility.FromJson<Pack>(System.IO.File.ReadAllText(LevelsJsonPath)); packTime = time; }
            catch { pack = null; }
        }
        return pack != null && pack.levels != null && pack.levels.Length > 0;
    }

    private void Styles()
    {
        if (chip != null) return;
        chip = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 2, 2) };
        chip.normal.textColor = Color.white;
        small = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        heading = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        cellText = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
        cellText.normal.textColor = Ink;
    }

    // ---------- navigation ----------
    private void DrawNav(int count, LevelJson level)
    {
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(Level <= 1)) if (GUILayout.Button("◀", GUILayout.Width(32), GUILayout.Height(22))) Level--;
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Level", GUILayout.Width(36));
        Level = Mathf.Clamp(EditorGUILayout.IntField(Level, GUILayout.Width(44)), 1, count);
        EditorGUILayout.LabelField("/ " + count, GUILayout.Width(40));
        GUILayout.FlexibleSpace();
        using (new EditorGUI.DisabledScope(Level >= count)) if (GUILayout.Button("▶", GUILayout.Width(32), GUILayout.Height(22))) Level++;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("“" + level.title + "”", heading);
    }

    // the levels around the current one, as small boards; click one to open it
    private void DrawStrip(int count, Color accent)
    {
        const int span = 4;
        int from = Mathf.Clamp(Level - span, 1, Mathf.Max(1, count - span * 2)), to = Mathf.Min(count, from + span * 2);
        var row = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
        int n = to - from + 1;
        float w = Mathf.Min(64f, (row.width - (n - 1) * 4f) / n);
        float x = row.x + (row.width - (n * w + (n - 1) * 4f)) * .5f;
        for (int i = from; i <= to; i++, x += w + 4f)
        {
            var r = new Rect(x, row.y, w, 52f);
            if (i == Level) Round(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), accent);
            Round(r, Card);
            DrawCells(pack.levels[i - 1], Inset(r, 4f), true, false);
            GUI.Label(new Rect(r.x, r.yMax, r.width, 12f), i.ToString(), small);
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { Level = i; Event.current.Use(); }
        }
    }

    // ---------- the board ----------
    private void DrawBoard(LevelJson level, float width)
    {
        float aspect = level.rows / (float)Mathf.Max(1, level.cols);
        float w = width, h = width * aspect;
        if (h > 420f) { h = 420f; w = h / aspect; }
        var outer = GUILayoutUtility.GetRect(0f, h + 24f, GUILayout.ExpandWidth(true));
        var card = new Rect(outer.x + (outer.width - w - 24f) * .5f, outer.y, w + 24f, h + 24f);
        Round(card, CardEdge);
        Round(Inset(card, 3f), Card);
        DrawCells(level, Inset(card, 12f), false, showCats);
    }

    private void DrawCells(LevelJson level, Rect area, bool thumb, bool cats)
    {
        if (level.rows <= 0 || level.cols <= 0) return;
        float size = Mathf.Min(area.width / level.cols, area.height / level.rows);
        var origin = new Vector2(area.x + (area.width - size * level.cols) * .5f, area.y + (area.height - size * level.rows) * .5f);
        Rect At(int r, int c) => new Rect(origin.x + c * size, origin.y + r * size, size, size);
        float gap = thumb ? .5f : Mathf.Max(1f, size * .06f);

        // the board is every cell a cat (or a stone) covers in the solution
        var owner = new Dictionary<(int, int), int>();
        if (level.pieces != null)
            for (int i = 0; i < level.pieces.Length; i++)
                foreach (var cell in ShapeCells(level.pieces[i])) owner[cell] = i;
        var stones = new HashSet<(int, int)>();
        if (level.blocked != null) foreach (var b in level.blocked) stones.Add((b.row, b.col));

        foreach (var kv in owner)
        {
            var r = Inset(At(kv.Key.Item1, kv.Key.Item2), gap);
            var piece = level.pieces[kv.Value];
            if (cats || (thumb && piece.@fixed)) Round(r, CatColors[kv.Value % CatColors.Length]);
            else { Round(r, CellEdge); Round(Inset(r, thumb ? .5f : 1.5f), Cell); }
        }
        foreach (var s in stones)
        {
            var r = Inset(At(s.Item1, s.Item2), gap);
            Round(r, Stone);
            if (!thumb) { Round(Inset(r, size * .18f), Hex("A39C96")); }
        }
        if (thumb) return;

        // colour zones: a ring in the colour of the cat the spot is kept for
        if (level.zones != null)
            foreach (var z in level.zones)
            {
                int idx = level.pieces != null ? Array.FindIndex(level.pieces, p => p.id == z.piece) : -1;
                var r = Inset(At(z.row, z.col), gap);
                Ring(r, idx >= 0 ? CatColors[idx % CatColors.Length] : Color.white, Mathf.Max(2f, size * .08f));
            }
        // fog
        if (level.fog != null)
            foreach (var f in level.fog) EditorGUI.DrawRect(Inset(At(f.row, f.col), gap), new Color(1f, 1f, 1f, .55f));
        // padlocks, with the number of placed cats that opens each one
        if (level.locked != null)
            foreach (var l in level.locked)
            {
                var r = Inset(At(l.row, l.col), size * .22f);
                Round(r, LockColor);
                GUI.Label(r, l.after > 0 ? l.after.ToString() : "🔒", cellText);
            }
        // the cats' names on their first cell
        if (cats && level.pieces != null && size >= 34f)
            for (int i = 0; i < level.pieces.Length; i++)
            {
                var cells = ShapeCells(level.pieces[i]);
                if (cells.Count == 0) continue;
                var first = cells[0];
                GUI.Label(At(first.Item1, first.Item2), level.pieces[i].name.Length > 5 ? level.pieces[i].name.Substring(0, 5) : level.pieces[i].name, cellText);
            }
    }

    // ---------- the tray ----------
    private void DrawTray(LevelJson level)
    {
        var all = new List<(PieceJson p, int colour, bool spare)>();
        if (level.pieces != null) for (int i = 0; i < level.pieces.Length; i++) if (!level.pieces[i].@fixed) all.Add((level.pieces[i], i, false));
        if (level.spares != null) foreach (var s in level.spares) all.Add((s, -1, true));
        if (all.Count == 0) return;
        EditorGUILayout.LabelField("Cats in the tray" + (Len(level.spares) > 0 ? " (red = spare, not part of the solution)" : ""), EditorStyles.miniBoldLabel);
        const float tile = 58f;
        var row = GUILayoutUtility.GetRect(0f, tile + 14f, GUILayout.ExpandWidth(true));
        int perRow = Mathf.Max(1, Mathf.FloorToInt(row.width / (tile + 6f)));
        int rows = Mathf.CeilToInt(all.Count / (float)perRow);
        if (rows > 1) { var extra = GUILayoutUtility.GetRect(0f, (tile + 14f) * (rows - 1), GUILayout.ExpandWidth(true)); }
        for (int i = 0; i < all.Count; i++)
        {
            var r = new Rect(row.x + (i % perRow) * (tile + 6f), row.y + (i / perRow) * (tile + 14f), tile, tile);
            Round(r, Card);
            var cells = ShapeCells(all[i].p, true);
            int maxR = 0, maxC = 0;
            foreach (var c in cells) { maxR = Mathf.Max(maxR, c.Item1); maxC = Mathf.Max(maxC, c.Item2); }
            float s = Mathf.Min((tile - 10f) / (maxC + 1), (tile - 10f) / (maxR + 1));
            float ox = r.x + (tile - s * (maxC + 1)) * .5f, oy = r.y + (tile - s * (maxR + 1)) * .5f;
            var colour = all[i].spare ? Hex("F07A7A") : CatColors[all[i].colour % CatColors.Length];
            foreach (var c in cells) Round(Inset(new Rect(ox + c.Item2 * s, oy + c.Item1 * s, s, s), 1f), colour);
            GUI.Label(new Rect(r.x - 4f, r.yMax, tile + 8f, 12f), all[i].p.name, small);
        }
    }

    // ---------- shapes (the game's own shape table) ----------
    private static MethodInfo tryGetShape;
    private static FieldInfo rowField, colField;

    private static List<(int, int)> ShapeCells(PieceJson p, bool local = false)
    {
        var cells = new List<(int, int)>();
        if (tryGetShape == null)
        {
            var lib = typeof(CatBlockPuzzle.GameSystem).Assembly.GetType("CatBlockPuzzle.ShapeLibrary");
            tryGetShape = lib?.GetMethod("TryGetShape", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var offset = typeof(CatBlockPuzzle.GameSystem).Assembly.GetType("CatBlockPuzzle.CellOffset");
            rowField = offset?.GetField("Row"); colField = offset?.GetField("Col");
        }
        if (tryGetShape == null || rowField == null) return cells;
        var args = new object[] { p.shape, null };
        if (!(bool)tryGetShape.Invoke(null, args) || !(args[1] is Array offsets)) return cells;
        foreach (var o in offsets)
        {
            int r = (int)rowField.GetValue(o), c = (int)colField.GetValue(o);
            cells.Add(local ? (r, c) : (p.row + r, p.col + c));
        }
        return cells;
    }

    // ---------- drawing helpers ----------
    private void Chip(string label, Color colour)
    {
        var size = chip.CalcSize(new GUIContent(label));
        var r = GUILayoutUtility.GetRect(size.x + 4f, 18f, GUILayout.ExpandWidth(false));
        Round(new Rect(r.x, r.y + 1f, size.x, 16f), colour);
        GUI.Label(new Rect(r.x, r.y + 1f, size.x, 16f), label, chip);
    }

    private static Rect Inset(Rect r, float d) => new Rect(r.x + d, r.y + d, r.width - d * 2f, r.height - d * 2f);

    private static void Round(Rect r, Color colour)
    {
        if (Event.current.type != EventType.Repaint) return;
        float radius = Mathf.Min(r.width, r.height) * .22f;
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, colour, 0f, radius);
    }

    private static void Ring(Rect r, Color colour, float width)
    {
        if (Event.current.type != EventType.Repaint) return;
        float radius = Mathf.Min(r.width, r.height) * .22f;
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, colour, width, radius);
    }
}
