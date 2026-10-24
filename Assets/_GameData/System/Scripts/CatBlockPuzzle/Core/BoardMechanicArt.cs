using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Draws the art for the Level Plan v2 board mechanics at runtime, in the same warm palette
    /// as <see cref="CellLockArt"/>: the cat food bowl on blocked cells, the fog cloud, the
    /// "settled" pin on a fixed cat and the Rotate power-up arrow.
    /// </summary>
    internal static class BoardMechanicArt
    {
        private static readonly Color Outline = new Color(0.36f, 0.19f, 0.10f, 1f);

        private static Sprite bowl;
        private static Sprite cloud;
        private static Sprite pin;
        private static Sprite rotate;

        public static Sprite Bowl
        {
            get
            {
                if (bowl == null) bowl = CreateBowl(128);
                return bowl;
            }
        }

        public static Sprite Cloud
        {
            get
            {
                if (cloud == null) cloud = CreateCloud(128);
                return cloud;
            }
        }

        public static Sprite Pin
        {
            get
            {
                if (pin == null) pin = CreatePin(96);
                return pin;
            }
        }

        public static Sprite Rotate
        {
            get
            {
                if (rotate == null) rotate = CreateRotate(128);
                return rotate;
            }
        }

        private static float Circle(Vector2 p, Vector2 center, float radius) => Vector2.Distance(p, center) - radius;

        private static float Ellipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            Vector2 q = new Vector2((p.x - center.x) / radii.x, (p.y - center.y) / radii.y);
            return (q.magnitude - 1f) * Mathf.Min(radii.x, radii.y);
        }

        private static float Box(Vector2 p, Vector2 center, Vector2 half)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        }

        private static Texture2D NewTexture(int size, string name) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        private static Sprite Finish(Texture2D texture, Color32[] pixels, int size)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // A round pink bowl seen from the front, heaped with brown kibble and a fish-bone tag.
        private static Sprite CreateBowl(int size)
        {
            Texture2D texture = NewTexture(size, "Cat Bowl");
            Color32[] pixels = new Color32[size * size];
            Color bowlTop = new Color(1f, 0.62f, 0.66f, 1f);
            Color bowlShade = new Color(0.88f, 0.42f, 0.50f, 1f);
            Color rim = new Color(1f, 0.80f, 0.82f, 1f);
            Color kibble = new Color(0.72f, 0.45f, 0.22f, 1f);
            Color kibbleLight = new Color(0.88f, 0.62f, 0.32f, 1f);

            float s = size;
            float outlineWidth = 0.035f * s;
            Vector2 bodyCenter = new Vector2(0.5f, 0.36f) * s;
            Vector2 bodyRadii = new Vector2(0.40f, 0.22f) * s;
            Vector2 rimCenter = new Vector2(0.5f, 0.47f) * s;
            Vector2 rimRadii = new Vector2(0.42f, 0.09f) * s;
            Vector2 heapCenter = new Vector2(0.5f, 0.50f) * s;
            Vector2 heapRadii = new Vector2(0.33f, 0.14f) * s;
            Vector2[] bits =
            {
                new Vector2(0.36f, 0.55f), new Vector2(0.50f, 0.60f), new Vector2(0.63f, 0.55f),
                new Vector2(0.43f, 0.50f), new Vector2(0.57f, 0.50f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color color = Color.clear;

                    // Kibble heap first, so the bowl's front edge overlaps it.
                    float heap = Ellipse(p, heapCenter, heapRadii);
                    if (p.y >= rimCenter.y - 0.02f * s && heap < 1f)
                    {
                        float fill = 1f - Mathf.Clamp01(heap);
                        float inner = 1f - Mathf.Clamp01(heap + outlineWidth);
                        Color body = kibble;
                        foreach (Vector2 bit in bits)
                            if (Circle(p, bit * s, 0.045f * s) < 0f) body = kibbleLight;
                        color = Color.Lerp(Outline, body, inner);
                        color.a = fill;
                    }

                    // Bowl body: the lower half of an ellipse, cut flat at the rim.
                    float bodyDist = Mathf.Max(Ellipse(p, bodyCenter, bodyRadii), p.y - rimCenter.y);
                    float rimDist = Ellipse(p, rimCenter, rimRadii);
                    float d = Mathf.Min(bodyDist, Mathf.Max(rimDist, rimCenter.y - p.y));
                    if (d < 1f)
                    {
                        float fill = 1f - Mathf.Clamp01(d);
                        float inner = 1f - Mathf.Clamp01(d + outlineWidth);
                        float shade = Mathf.InverseLerp(rimCenter.y, bodyCenter.y - bodyRadii.y, p.y);
                        Color body = rimDist < 0f && p.y > rimCenter.y - 0.01f * s ? rim : Color.Lerp(bowlTop, bowlShade, shade);
                        Color painted = Color.Lerp(Outline, body, inner);
                        color = Color.Lerp(color, painted, fill);
                        color.a = Mathf.Max(color.a, fill);
                    }

                    pixels[y * size + x] = color;
                }
            }

            return Finish(texture, pixels, size);
        }

        // A soft, puffy white-blue cloud that fills most of the cell.
        private static Sprite CreateCloud(int size)
        {
            Texture2D texture = NewTexture(size, "Fog Cloud");
            Color32[] pixels = new Color32[size * size];
            Color top = new Color(0.98f, 0.99f, 1f, 1f);
            Color bottom = new Color(0.80f, 0.86f, 0.95f, 1f);
            Color edge = new Color(0.62f, 0.70f, 0.84f, 1f);

            float s = size;
            Vector2[] puffs =
            {
                new Vector2(0.30f, 0.46f), new Vector2(0.50f, 0.58f), new Vector2(0.70f, 0.47f),
                new Vector2(0.42f, 0.34f), new Vector2(0.60f, 0.33f), new Vector2(0.50f, 0.42f)
            };
            float[] radii = { 0.21f, 0.25f, 0.21f, 0.20f, 0.20f, 0.24f };
            float soft = 0.05f * s;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = float.MaxValue;
                    for (int i = 0; i < puffs.Length; i++) d = Mathf.Min(d, Circle(p, puffs[i] * s, radii[i] * s));
                    float fill = 1f - Mathf.Clamp01(d / 1.5f);
                    float inner = 1f - Mathf.Clamp01((d + soft) / soft);
                    float shade = Mathf.InverseLerp(0.75f * s, 0.15f * s, p.y);
                    Color body = Color.Lerp(top, bottom, shade);
                    Color color = Color.Lerp(edge, body, inner);
                    color.a = fill;
                    pixels[y * size + x] = color;
                }
            }

            return Finish(texture, pixels, size);
        }

        // A red map pin: round head with a highlight over a short tapered needle.
        private static Sprite CreatePin(int size)
        {
            Texture2D texture = NewTexture(size, "Settled Pin");
            Color32[] pixels = new Color32[size * size];
            Color head = new Color(0.95f, 0.33f, 0.33f, 1f);
            Color headShade = new Color(0.78f, 0.20f, 0.24f, 1f);
            Color shine = new Color(1f, 0.82f, 0.80f, 1f);

            float s = size;
            Vector2 headCenter = new Vector2(0.5f, 0.62f) * s;
            float headRadius = 0.27f * s;
            float outlineWidth = 0.06f * s;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    // Needle: a triangle from the head down to a point at the bottom centre.
                    float t = Mathf.InverseLerp(0.08f * s, headCenter.y, p.y);
                    float needleHalf = Mathf.Lerp(0.01f, 0.16f, t) * s;
                    float needle = p.y < headCenter.y && p.y > 0.08f * s ? Mathf.Abs(p.x - headCenter.x) - needleHalf : float.MaxValue;
                    float d = Mathf.Min(Circle(p, headCenter, headRadius), needle);
                    float fill = 1f - Mathf.Clamp01(d);
                    float inner = 1f - Mathf.Clamp01(d + outlineWidth);
                    float shade = Mathf.InverseLerp(headCenter.y + headRadius, headCenter.y - headRadius, p.y);
                    Color body = Color.Lerp(head, headShade, shade);
                    if (Circle(p, headCenter + new Vector2(-0.09f, 0.09f) * s, 0.07f * s) < 0f) body = shine;
                    Color color = Color.Lerp(Outline, body, inner);
                    color.a = fill;
                    pixels[y * size + x] = color;
                }
            }

            return Finish(texture, pixels, size);
        }

        // A thick clockwise arrow around a circle, in the game's gold.
        private static Sprite CreateRotate(int size)
        {
            Texture2D texture = NewTexture(size, "Rotate Arrow");
            Color32[] pixels = new Color32[size * size];
            Color gold = new Color(1f, 0.82f, 0.32f, 1f);
            Color goldShade = new Color(0.92f, 0.58f, 0.18f, 1f);

            float s = size;
            Vector2 center = new Vector2(0.5f, 0.5f) * s;
            float radius = 0.30f * s;
            float thickness = 0.11f * s;
            float outlineWidth = 0.035f * s;
            // The ring is open between these angles; the arrow head sits at the gap's end.
            const float gapStart = 60f, gapEnd = 110f;
            Vector2 tip = center + new Vector2(Mathf.Cos(gapStart * Mathf.Deg2Rad), Mathf.Sin(gapStart * Mathf.Deg2Rad)) * radius;
            Vector2 tangent = new Vector2(Mathf.Sin(gapStart * Mathf.Deg2Rad), -Mathf.Cos(gapStart * Mathf.Deg2Rad));
            Vector2 normal = (tip - center).normalized;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 v = p - center;
                    float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    float ring = Mathf.Abs(v.magnitude - radius) - thickness * 0.5f;
                    if (angle > gapStart && angle < gapEnd) ring = float.MaxValue;

                    // Arrow head: a triangle pointing clockwise along the ring at the start of the gap.
                    Vector2 local = new Vector2(Vector2.Dot(p - tip, tangent), Vector2.Dot(p - tip, normal));
                    float headLength = 0.2f * s, headHalf = 0.17f * s;
                    float along = local.x + headLength * 0.35f;
                    float head = along < 0f || along > headLength ? float.MaxValue
                        : Mathf.Abs(local.y) - headHalf * (1f - along / headLength);

                    float d = Mathf.Min(ring, head);
                    float fill = 1f - Mathf.Clamp01(d);
                    float inner = 1f - Mathf.Clamp01(d + outlineWidth);
                    float shade = Mathf.InverseLerp(0.8f * s, 0.2f * s, p.y);
                    Color body = Color.Lerp(gold, goldShade, shade * shade);
                    Color color = Color.Lerp(Outline, body, inner);
                    color.a = fill;
                    pixels[y * size + x] = color;
                }
            }

            return Finish(texture, pixels, size);
        }
    }
}
