using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Draws the padlock used on locked board cells at runtime, in the game's warm gold and
    /// cocoa palette, so no authored sprite or scene re-preparation is needed.
    /// </summary>
    internal static class CellLockArt
    {
        private static Sprite padlock;

        public static Sprite Padlock
        {
            get
            {
                if (padlock == null) padlock = CreatePadlock(128);
                return padlock;
            }
        }

        private static Sprite CreatePadlock(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Cell Padlock" };
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];

            Color outline = new Color(0.36f, 0.19f, 0.10f, 1f);
            Color body = new Color(0.98f, 0.76f, 0.30f, 1f);
            Color bodyShade = new Color(0.90f, 0.60f, 0.20f, 1f);
            Color shackle = new Color(0.62f, 0.62f, 0.66f, 1f);
            Color shackleShade = new Color(0.42f, 0.42f, 0.48f, 1f);
            Color keyhole = new Color(0.30f, 0.16f, 0.09f, 1f);

            float s = size;
            Vector2 bodyCenter = new Vector2(0.5f, 0.36f) * s;
            Vector2 bodyHalf = new Vector2(0.30f, 0.24f) * s;
            float bodyRadius = 0.09f * s;
            Vector2 shackleCenter = new Vector2(0.5f, 0.62f) * s;
            float shackleRadius = 0.19f * s;
            float shackleThickness = 0.075f * s;
            float outlineWidth = 0.035f * s;
            Vector2 holeCenter = new Vector2(0.5f, 0.40f) * s;
            float holeRadius = 0.055f * s;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float aa = 1f;

                    // Shackle: the upper half of a ring, drawn first so the body overlaps it.
                    float ring = Mathf.Abs(Vector2.Distance(p, shackleCenter) - shackleRadius) - shackleThickness * 0.5f;
                    bool shackleZone = p.y >= shackleCenter.y - 0.02f * s;
                    float shackleFill = shackleZone ? 1f - Mathf.Clamp01(ring / aa) : 0f;
                    float shackleEdge = shackleZone ? 1f - Mathf.Clamp01((ring + outlineWidth) / aa) : 0f;

                    // Body: rounded rectangle.
                    Vector2 d = new Vector2(Mathf.Abs(p.x - bodyCenter.x), Mathf.Abs(p.y - bodyCenter.y)) - (bodyHalf - Vector2.one * bodyRadius);
                    float box = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - bodyRadius;
                    float bodyFill = 1f - Mathf.Clamp01(box / aa);
                    float bodyEdge = 1f - Mathf.Clamp01((box + outlineWidth) / aa);

                    // Keyhole: a circle with a slot below it.
                    float hole = Vector2.Distance(p, holeCenter) - holeRadius;
                    float slot = Mathf.Max(Mathf.Abs(p.x - holeCenter.x) - 0.03f * s, Mathf.Max(holeCenter.y - 0.10f * s - p.y, p.y - holeCenter.y));
                    float holeFill = 1f - Mathf.Clamp01(Mathf.Min(hole, slot) / aa);

                    Color color = Color.clear;
                    if (shackleFill > 0f)
                    {
                        float shade = Mathf.InverseLerp(shackleCenter.y + shackleRadius, shackleCenter.y - shackleThickness, p.y);
                        Color metal = Color.Lerp(shackle, shackleShade, shade);
                        color = Color.Lerp(outline, metal, shackleEdge);
                        color.a = shackleFill;
                    }

                    if (bodyFill > 0f)
                    {
                        float shade = Mathf.InverseLerp(bodyCenter.y + bodyHalf.y, bodyCenter.y - bodyHalf.y, p.y);
                        Color gold = Color.Lerp(body, bodyShade, shade * shade);
                        Color inner = Color.Lerp(outline, gold, bodyEdge);
                        inner = Color.Lerp(inner, keyhole, holeFill);
                        color = Color.Lerp(color, inner, bodyFill);
                        color.a = Mathf.Max(color.a, bodyFill);
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
