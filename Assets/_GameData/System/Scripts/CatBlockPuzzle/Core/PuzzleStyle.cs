using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Colours shared by the gameplay presentation, tutorial, rooms and editor authoring.</summary>
    internal static class PuzzleStyle
    {
        public static readonly Color PageColor = new Color(251f / 255f, 247f / 255f, 238f / 255f, 1f);
        public static readonly Color PanelColor = new Color(1f, 248f / 255f, 236f / 255f, 0.97f);
        public static readonly Color TrayColor = new Color(248f / 255f, 184f / 255f, 153f / 255f, 0.98f);
        public static readonly Color InkColor = new Color(74f / 255f, 46f / 255f, 42f / 255f, 1f);
        public static readonly Color SoftInkColor = new Color(0.52f, 0.43f, 0.35f, 1f);
        public static readonly Color TargetColor = new Color(1f, .94f, .82f, 1f);
        public static readonly Color TargetDeepColor = new Color(59f / 255f, 151f / 255f, 137f / 255f, 1f);
        public static readonly Color ValidColor = new Color(0.68f, 0.88f, 0.65f, 1f);
        public static readonly Color InvalidColor = new Color(1f, 0.55f, 0.58f, 1f);
        public static readonly Color LockedColor = new Color(.68f, .55f, .39f, 1f);
        public static readonly Color GoldColor = new Color(246f / 255f, 190f / 255f, 62f / 255f, 1f);
        public static readonly Color PanelOutlineColor = new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.78f);
        public static readonly Color BoardFrameColor = new Color(251f / 255f, 241f / 255f, 221f / 255f, 1f);
        public static readonly Color BoardOutlineColor = new Color(230f / 255f, 214f / 255f, 184f / 255f, 0.9f);
        public static readonly Color BoardTileEdgeColor = new Color(.78f, .51f, .22f, .45f);
        public static readonly Color CardRestColor = new Color(1f, 252f / 255f, 246f / 255f, 0.92f);
        public static readonly Color CardDimColor = new Color(1f, 248f / 255f, 236f / 255f, 0.42f);
        public static readonly Color TrayHoverColor = new Color(1f, 207f / 255f, 164f / 255f, 1f);
        public static readonly Color TimerNormalColor = new Color(0.05f, 0.045f, 0.04f, 1f);
        public static readonly Color TimerWarningColor = new Color(0.88f, 0.08f, 0.08f, 1f);

        public static readonly Color[] PieceColors =
        {
            new Color(1f, 143f / 255f, 166f / 255f, 1f),
            new Color(102f / 255f, 208f / 255f, 183f / 255f, 1f),
            new Color(111f / 255f, 167f / 255f, 245f / 255f, 1f),
            new Color(246f / 255f, 190f / 255f, 62f / 255f, 1f),
            new Color(0.74f, 0.65f, 0.94f, 1f),
            new Color(0.95f, 0.55f, 0.45f, 1f),
            new Color(0.61f, 0.83f, 0.42f, 1f),
            new Color(0.44f, 0.78f, 0.9f, 1f)
        };
    }
}
