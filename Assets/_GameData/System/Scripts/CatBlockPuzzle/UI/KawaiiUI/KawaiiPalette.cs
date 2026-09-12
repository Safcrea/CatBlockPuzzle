using UnityEngine;

namespace CatBlockPuzzle.KawaiiUI
{
    public static class KawaiiPalette
    {
        public static readonly Color BackgroundCream = Hex("#FBF7EE");
        public static readonly Color BackgroundWarm = Hex("#F6EEDB");
        public static readonly Color BoardFrameMint = Hex("#FBF1DD");
        public static readonly Color BoardTileGreen = Hex("#D9D9DD");
        public static readonly Color EmptyCellCream = Hex("#FFF8E8");
        public static readonly Color TextDarkBrown = Hex("#4A2E2A");
        public static readonly Color SoftBeige = Hex("#E5D0A8");
        public static readonly Color WhitePanel = Hex("#FFF8EC");
        public static readonly Color PinkPiece = Hex("#FF8FA6");
        public static readonly Color MintPiece = Hex("#66D0B7");
        public static readonly Color BluePiece = Hex("#6FA7F5");
        public static readonly Color YellowPiece = Hex("#F6BE3E");
        public static readonly Color ValidGlow = Hex("#84B94D");
        public static readonly Color InvalidGlow = Hex("#FF8FA6");

        public static Color Hex(string html)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(html, out color))
            {
                return color;
            }

            return Color.magenta;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
