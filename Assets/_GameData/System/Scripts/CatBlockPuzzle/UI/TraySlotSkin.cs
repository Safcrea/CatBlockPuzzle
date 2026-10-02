using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The look of every tray slot (the cards the cats sit in before they are dragged), copied from
    /// one authored skin prefab (Sunny Slot) onto each level's slots as the level loads. Editing that
    /// prefab restyles the slots of all 100 levels. The cats inside each slot are never touched.
    /// Without a skin the slots keep the original cream style.
    /// </summary>
    internal static class TraySlotSkin
    {
        // Same rest → dim fade ratio the original cream cards used (0.92 → 0.42 alpha).
        private const float DimAlphaRatio = 0.42f / 0.92f;

        private static Image skinImage;
        private static Shadow skinShadow;
        private static Outline skinOutline;
        private static Color? skinDotColor;

        public static Color RestColor { get; private set; } = PuzzleStyle.CardRestColor;
        public static Color DimColor { get; private set; } = PuzzleStyle.CardDimColor;
        public static bool HasSkin => skinImage != null;

        public static void Use(GameObject skin)
        {
            skinImage = null; skinShadow = null; skinOutline = null; skinDotColor = null;
            RestColor = PuzzleStyle.CardRestColor;
            DimColor = PuzzleStyle.CardDimColor;
            if (skin == null || !skin.TryGetComponent(out skinImage)) return;
            foreach (Shadow effect in skin.GetComponents<Shadow>())
            {
                if (effect is Outline outline) skinOutline = outline;
                else skinShadow = effect;
            }
            Transform dots = skin.transform.Find("Drag Dots");
            if (dots != null && dots.childCount > 0 && dots.GetChild(0).TryGetComponent(out Image dot)) skinDotColor = dot.color;
            RestColor = skinImage.color;
            DimColor = new Color(RestColor.r, RestColor.g, RestColor.b, RestColor.a * DimAlphaRatio);
        }

        /// <summary>Restyles one slot: background, shadow, border and drag dots.</summary>
        public static void Apply(RectTransform slot, Image slotImage)
        {
            if (!HasSkin || slot == null) return;
            if (slotImage != null)
            {
                slotImage.sprite = skinImage.sprite;
                slotImage.type = skinImage.type;
                slotImage.pixelsPerUnitMultiplier = skinImage.pixelsPerUnitMultiplier;
                slotImage.color = RestColor;
            }
            foreach (Shadow effect in slot.GetComponents<Shadow>())
            {
                Shadow source = effect is Outline ? skinOutline : skinShadow;
                if (source == null) continue;
                effect.effectColor = source.effectColor;
                effect.effectDistance = source.effectDistance;
                effect.useGraphicAlpha = source.useGraphicAlpha;
            }
            Transform dots = slot.Find("Drag Dots");
            if (dots != null && skinDotColor.HasValue)
                foreach (Transform dot in dots)
                    if (dot.TryGetComponent(out Image dotImage)) dotImage.color = skinDotColor.Value;
        }
    }
}
