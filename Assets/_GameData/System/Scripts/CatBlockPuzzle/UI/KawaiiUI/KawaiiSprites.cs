using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle.KawaiiUI
{
    public static class KawaiiSprites
    {
        private const float PixelsPerUnit = 100f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite RoundedRect
        {
            get { return Get("rounded_rect", CreateRoundedRectTexture(96, 96, 24f), new Vector4(24f, 24f, 24f, 24f)); }
        }

        public static Sprite Circle
        {
            get { return Get("circle", CreateCircleTexture(96), Vector4.zero); }
        }

        public static Sprite Triangle
        {
            get { return Get("triangle", CreateTriangleTexture(96), Vector4.zero); }
        }

        public static Sprite Paw
        {
            get { return Get("paw", CreatePawTexture(96), Vector4.zero); }
        }

        public static Sprite Star
        {
            get { return Get("star", CreateStarTexture(128), Vector4.zero); }
        }

        public static Sprite BackgroundGradient
        {
            get { return Get("background_gradient", CreateBackgroundGradientTexture(512, 512), Vector4.zero); }
        }

        public static Texture2D CreateRoundedRectTexture(int width, int height, float radius)
        {
            Texture2D texture = NewTexture(width, height);
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            Vector2 half = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 q = new Vector2(Mathf.Abs(point.x - center.x), Mathf.Abs(point.y - center.y));
                    q -= half - new Vector2(radius, radius);
                    float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                    float distance = outside + inside - radius;
                    float alpha = Mathf.Clamp01(1f - (distance / 1.6f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.43f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(1f - ((distance - radius) / 2.4f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D CreateTriangleTexture(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Vector2 a = new Vector2(size * 0.5f, size * 0.88f);
            Vector2 b = new Vector2(size * 0.1f, size * 0.1f);
            Vector2 c = new Vector2(size * 0.9f, size * 0.1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = PointInTriangle(point, a, b, c) ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D CreatePawTexture(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Vector2 pad = new Vector2(size * 0.5f, size * 0.34f);
            Vector2[] toes =
            {
                new Vector2(size * 0.29f, size * 0.65f),
                new Vector2(size * 0.43f, size * 0.74f),
                new Vector2(size * 0.57f, size * 0.74f),
                new Vector2(size * 0.71f, size * 0.65f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = EllipseAlpha(point, pad, size * 0.19f, size * 0.18f, 2.5f);
                    for (int i = 0; i < toes.Length; i++)
                    {
                        alpha = Mathf.Max(alpha, EllipseAlpha(point, toes[i], size * 0.095f, size * 0.1f, 2.2f));
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D CreateStarTexture(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.42f;
            float inner = size * 0.2f;
            Vector2[] points = new Vector2[10];

            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.PI * 0.5f + (Mathf.PI * 2f * i / points.Length);
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = PointInPolygon(point, points) ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D CreateBackgroundGradientTexture(int width, int height)
        {
            Texture2D texture = NewTexture(width, height);
            Color top = KawaiiPalette.BackgroundCream;
            Color bottom = KawaiiPalette.BackgroundWarm;
            Color pink = KawaiiPalette.WithAlpha(KawaiiPalette.PinkPiece, 0.2f);
            Color mint = KawaiiPalette.WithAlpha(KawaiiPalette.MintPiece, 0.16f);

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    Color color = Color.Lerp(bottom, top, v);
                    color = Color.Lerp(color, pink, Radial(u, v, 0.18f, 0.82f, 0.42f));
                    color = Color.Lerp(color, mint, Radial(u, v, 0.84f, 0.28f, 0.46f));
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Sprite Get(string key, Texture2D texture, Vector4 border)
        {
            Sprite sprite;
            if (Cache.TryGetValue(key, out sprite) && sprite != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(texture);
                }
                else
                {
                    Object.DestroyImmediate(texture);
                }

                return sprite;
            }

            texture.name = "KawaiiUI_" + key;
            texture.hideFlags = HideFlags.HideAndDontSave;
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = texture.name + "_Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static float Radial(float u, float v, float cx, float cy, float radius)
        {
            float dx = u - cx;
            float dy = v - cy;
            float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
            return Mathf.Clamp01(1f - (distance / Mathf.Max(0.01f, radius)));
        }

        private static float EllipseAlpha(Vector2 point, Vector2 center, float radiusX, float radiusY, float softness)
        {
            float dx = (point.x - center.x) / Mathf.Max(0.01f, radiusX);
            float dy = (point.y - center.y) / Mathf.Max(0.01f, radiusY);
            float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
            return Mathf.Clamp01(1f - ((distance - 1f) * Mathf.Max(radiusX, radiusY) / Mathf.Max(0.01f, softness)));
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(point, a, b);
            float d2 = Sign(point, b, c);
            float d3 = Sign(point, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool crosses = polygon[i].y > point.y != polygon[j].y > point.y;
                if (crosses)
                {
                    float x = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
                    if (point.x < x)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return ((p1.x - p3.x) * (p2.y - p3.y)) - ((p2.x - p3.x) * (p1.y - p3.y));
        }
    }
}
