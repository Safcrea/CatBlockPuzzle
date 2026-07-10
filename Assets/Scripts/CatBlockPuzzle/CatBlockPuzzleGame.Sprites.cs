using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private enum UiIcon
        {
            Back,
            Pause,
            Settings,
            Hint,
            Reset,
            Close
        }

        private Sprite CreateSolidSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite CreateRoundedBoxSprite()
        {
            const int size = 64;
            const float radius = 14f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = RoundedRectAlpha(point, size, size, radius, 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }

        private float RoundedRectAlpha(Vector2 point, float width, float height, float radius, float softness)
        {
            Vector2 half = new Vector2(width * 0.5f, height * 0.5f);
            Vector2 q = new Vector2(Mathf.Abs(point.x - half.x) - (half.x - radius), Mathf.Abs(point.y - half.y) - (half.y - radius));
            Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            float signedDistance = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
            return Mathf.Clamp01(0.5f - (signedDistance / Mathf.Max(0.01f, softness)));
        }

        private Sprite CreateMouthSprite()
        {
            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(width * 0.5f, height * 0.78f);
            const float radiusX = 22f;
            const float radiusY = 13f;
            const float lineWidth = 2.2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float normalized = Mathf.Sqrt(Mathf.Pow((point.x - center.x) / radiusX, 2f) + Mathf.Pow((point.y - center.y) / radiusY, 2f));
                    float alpha = point.y <= center.y ? Mathf.Clamp01(1f - (Mathf.Abs(normalized - 1f) * Mathf.Max(radiusX, radiusY) / lineWidth)) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
        }

        private Sprite CreateTailSprite()
        {
            const int width = 72;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(width * 0.36f, height * 0.42f);
            const float radiusX = 25f;
            const float radiusY = 16f;
            const float lineWidth = 4.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float normalized = Mathf.Sqrt(Mathf.Pow((point.x - center.x) / radiusX, 2f) + Mathf.Pow((point.y - center.y) / radiusY, 2f));
                    bool visibleArc = point.x >= center.x || point.y >= center.y;
                    float alpha = visibleArc ? Mathf.Clamp01(1f - (Mathf.Abs(normalized - 1f) * Mathf.Max(radiusX, radiusY) / lineWidth)) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
        }

        private Sprite CreateBackgroundSprite()
        {
            if (visualCatalog != null && visualCatalog.CozyRoomBackground != null)
            {
                Texture2D authored = visualCatalog.CozyRoomBackground;
                return Sprite.Create(
                    authored,
                    new Rect(0f, 0f, authored.width, authored.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
            }

            const int width = 256;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color top = PageColor;
            Color middle = new Color(249f / 255f, 243f / 255f, 230f / 255f, 1f);
            Color bottom = new Color(246f / 255f, 238f / 255f, 219f / 255f, 1f);
            Color pink = new Color(1f, 143f / 255f, 166f / 255f, 1f);
            Color gold = new Color(246f / 255f, 190f / 255f, 62f / 255f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    float diagonal = Mathf.Clamp01((u * 0.35f) + ((1f - v) * 0.65f));
                    Color baseColor = diagonal < 0.48f
                        ? Color.Lerp(top, middle, diagonal / 0.48f)
                        : Color.Lerp(middle, bottom, (diagonal - 0.48f) / 0.52f);
                    baseColor = Color.Lerp(baseColor, pink, RadialAlpha(u, v, 0.14f, 0.14f, 0.32f, 0.05f));
                    baseColor = Color.Lerp(baseColor, gold, RadialAlpha(u, v, 0.9f, 0.08f, 0.34f, 0.05f));
                    texture.SetPixel(x, y, baseColor);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 1f);
        }

        private float RadialAlpha(float u, float v, float centerX, float centerY, float radius, float strength)
        {
            float dx = u - centerX;
            float dy = v - centerY;
            float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
            return Mathf.Clamp01(1f - (distance / Mathf.Max(0.01f, radius))) * strength;
        }

        private Sprite CreateCircleSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = Mathf.Clamp01(1f - ((distance - 27f) / 4f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void LoadAuthoredCatPortraits()
        {
            if (visualCatalog == null)
            {
                return;
            }

            SliceCatAtlas(visualCatalog.NeutralCatAtlas, (int)CatMood.Neutral);
            SliceCatAtlas(visualCatalog.HappyCatAtlas, (int)CatMood.Happy);
            SliceCatAtlas(visualCatalog.WorriedCatAtlas, (int)CatMood.Worried);
        }

        private void SliceCatAtlas(Texture2D atlas, int moodIndex)
        {
            if (atlas == null || moodIndex < 0 || moodIndex >= catPortraitSprites.GetLength(0))
            {
                return;
            }

            int cellWidth = Mathf.Max(1, atlas.width / 4);
            int cellHeight = Mathf.Max(1, atlas.height / 2);
            for (int index = 0; index < 8; index++)
            {
                int column = index % 4;
                int rowFromTop = index / 4;
                float x = column * cellWidth;
                float y = rowFromTop == 0 ? atlas.height - cellHeight : 0f;
                catPortraitSprites[moodIndex, index] = Sprite.Create(
                    atlas,
                    new Rect(x, y, cellWidth, cellHeight),
                    new Vector2(0.5f, 0.5f),
                    cellHeight,
                    0,
                    SpriteMeshType.FullRect);
            }
        }

        private Sprite CreateStarSprite(bool outline)
        {
            const int size = 72;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = Vector2.one * (size * 0.5f);
            Vector2[] points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float radius = i % 2 == 0 ? 31f : 14f;
                float angle = Mathf.Deg2Rad * (90f + (i * 36f));
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = PointInPolygon(point, points);
                    float alpha = inside ? 1f : 0f;
                    if (outline && inside)
                    {
                        bool inner = PointInPolygon(point + new Vector2(4f, 0f), points) &&
                                     PointInPolygon(point + new Vector2(-4f, 0f), points) &&
                                     PointInPolygon(point + new Vector2(0f, 4f), points) &&
                                     PointInPolygon(point + new Vector2(0f, -4f), points);
                        alpha = inner ? 0f : 1f;
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int previous = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; previous = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[previous];
                bool crosses = (a.y > point.y) != (b.y > point.y) &&
                               point.x < ((b.x - a.x) * (point.y - a.y) / (b.y - a.y)) + a.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private Sprite CreateUiIconSprite(UiIcon icon)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = Vector2.one * (size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = IconAlpha(icon, point, center);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private float IconAlpha(UiIcon icon, Vector2 point, Vector2 center)
        {
            switch (icon)
            {
                case UiIcon.Back:
                    return Mathf.Max(
                        LineAlpha(point, new Vector2(42f, 32f), new Vector2(19f, 32f), 4f),
                        Mathf.Max(
                            LineAlpha(point, new Vector2(20f, 32f), new Vector2(32f, 20f), 4f),
                            LineAlpha(point, new Vector2(20f, 32f), new Vector2(32f, 44f), 4f)));
                case UiIcon.Pause:
                    return Mathf.Max(
                        RectangleAlpha(point, new Rect(21f, 17f, 7f, 30f), 2f),
                        RectangleAlpha(point, new Rect(36f, 17f, 7f, 30f), 2f));
                case UiIcon.Close:
                    return Mathf.Max(
                        LineAlpha(point, new Vector2(20f, 20f), new Vector2(44f, 44f), 4f),
                        LineAlpha(point, new Vector2(44f, 20f), new Vector2(20f, 44f), 4f));
                case UiIcon.Settings:
                {
                    float distance = Vector2.Distance(point, center);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 13f) / 3.5f);
                    float spokes = 0f;
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = Mathf.Deg2Rad * i * 45f;
                        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        spokes = Mathf.Max(spokes, LineAlpha(point, center + direction * 14f, center + direction * 22f, 4f));
                    }

                    return Mathf.Max(ring, spokes);
                }
                case UiIcon.Hint:
                {
                    float bulb = Mathf.Clamp01(1f - Mathf.Abs(Vector2.Distance(point, new Vector2(32f, 37f)) - 12f) / 3f);
                    float stem = RectangleAlpha(point, new Rect(28f, 17f, 8f, 13f), 2f);
                    return Mathf.Max(bulb, stem);
                }
                case UiIcon.Reset:
                {
                    Vector2 offset = point - center;
                    float distance = offset.magnitude;
                    float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                    float arc = angle > -110f && angle < 155f ? Mathf.Clamp01(1f - Mathf.Abs(distance - 16f) / 3.5f) : 0f;
                    float arrow = Mathf.Max(
                        LineAlpha(point, new Vector2(16f, 39f), new Vector2(17f, 27f), 3.5f),
                        LineAlpha(point, new Vector2(16f, 39f), new Vector2(28f, 38f), 3.5f));
                    return Mathf.Max(arc, arrow);
                }
                default:
                    return 0f;
            }
        }

        private float LineAlpha(Vector2 point, Vector2 start, Vector2 end, float width)
        {
            Vector2 segment = end - start;
            float lengthSquared = Mathf.Max(0.0001f, segment.sqrMagnitude);
            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            float distance = Vector2.Distance(point, start + segment * t);
            return Mathf.Clamp01(1f - ((distance - width * 0.5f) / 1.5f));
        }

        private float RectangleAlpha(Vector2 point, Rect rect, float softness)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Clamp01(1f - (Mathf.Sqrt((dx * dx) + (dy * dy)) / Mathf.Max(0.01f, softness)));
        }

        private Sprite CreateCoinSprite()
        {
            const int size = 72;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float distance = Vector2.Distance(point, center);
                    float alpha = Mathf.Clamp01(1f - ((distance - 32f) / 3f));
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - 27f) / 3f);
                    float highlight = RadialAlpha(x / (float)(size - 1), y / (float)(size - 1), 0.34f, 0.68f, 0.34f, 0.55f);
                    Color baseColor = Color.Lerp(new Color(0.92f, 0.56f, 0.1f, 1f), new Color(1f, 0.86f, 0.36f, 1f), 1f - (distance / 36f));
                    baseColor = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.62f, 1f), highlight);
                    baseColor = Color.Lerp(baseColor, new Color(1f, 0.93f, 0.5f, 1f), rim * 0.45f);
                    texture.SetPixel(x, y, new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreatePawSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 pad = new Vector2(32f, 22f);
            Vector2[] toes =
            {
                new Vector2(20f, 42f),
                new Vector2(28f, 48f),
                new Vector2(36f, 48f),
                new Vector2(44f, 42f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float alpha = EllipseAlpha(point, pad, 12f, 13f, 2.5f);
                    for (int i = 0; i < toes.Length; i++)
                    {
                        alpha = Mathf.Max(alpha, EllipseAlpha(point, toes[i], 6f, 6f, 2f));
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private float EllipseAlpha(Vector2 point, Vector2 center, float radiusX, float radiusY, float softness)
        {
            float dx = (point.x - center.x) / Mathf.Max(0.01f, radiusX);
            float dy = (point.y - center.y) / Mathf.Max(0.01f, radiusY);
            float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
            return Mathf.Clamp01(1f - ((distance - 1f) * Mathf.Max(radiusX, radiusY) / Mathf.Max(0.01f, softness)));
        }

        private Sprite CreateCatHeadSprite()
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            const float radius = 26f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(Mathf.Abs(px - (size * 0.5f)) - ((size * 0.5f) - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(py - (size * 0.5f)) - ((size * 0.5f) - radius), 0f);
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = Mathf.Clamp01(1f - ((distance - radius) / 3f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private AudioClip CreateToneClip(string name, float seconds, float volume, params float[] frequencies)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(seconds * AudioSampleRate));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float normalized = i / (float)samples;
                int frequencyIndex = Mathf.Min(frequencies.Length - 1, Mathf.FloorToInt(normalized * frequencies.Length));
                float frequency = frequencies[Mathf.Max(0, frequencyIndex)];
                phase += (Mathf.PI * 2f * frequency) / AudioSampleRate;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                data[i] = Mathf.Sin(phase) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, AudioSampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
