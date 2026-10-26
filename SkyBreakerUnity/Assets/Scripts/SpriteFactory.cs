using System.Collections.Generic;
using UnityEngine;

// Genera por código las texturas del juego: figuras básicas, brillos, cielos degradados,
// nubes, montañas, fábricas, engranes y tuberías. Así no hace falta importar imágenes.
public static class SpriteFactory
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Square => Cached("square", () => FromPixels(Fill(4, 4, (x, y) => Color.white), 4, 4, 4, FilterMode.Point));
    public static Sprite Circle => Cached("circle", () => MakeCircle(64, false));
    public static Sprite SoftCircle => Cached("soft", () => MakeCircle(64, true));

    static Sprite Cached(string key, System.Func<Sprite> make)
    {
        if (Cache.TryGetValue(key, out var s) && s != null) return s;
        s = make();
        Cache[key] = s;
        return s;
    }

    public static Sprite FromPixels(Color32[] px, int w, int h, float ppu, FilterMode filter, Vector2? pivot = null)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), ppu);
    }

    static Color32[] Fill(int w, int h, System.Func<int, int, Color> f)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = f(x, y);
        return px;
    }

    static Sprite MakeCircle(int size, bool soft)
    {
        float r = size / 2f;
        var px = Fill(size, size, (x, y) =>
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float a = soft ? Mathf.Clamp01(1f - d) : (d <= 1f ? 1f : 0f);
            if (soft) a *= a;
            return new Color(1f, 1f, 1f, a);
        });
        return FromPixels(px, size, size, size, FilterMode.Bilinear);
    }

    public static SpriteRenderer Shape(string name, Transform parent, Sprite sprite, Color color,
        Vector2 localPos, Vector2 scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // Degradado vertical de 1 unidad de ancho por 1 de alto.
    public static Sprite Gradient(Color top, Color bottom)
    {
        return Cached("grad" + top + bottom, () =>
        {
            const int s = 64;
            var px = Fill(s, s, (x, y) => Color.Lerp(bottom, top, y / (s - 1f)));
            return FromPixels(px, s, s, s, FilterMode.Bilinear);
        });
    }

    // Nube de pixel art con dos tonos (arriba claro, abajo sombreado).
    public static Sprite PixelCloud(int seed, Color light, Color shade, float unitsWide)
    {
        return Cached("cloud" + seed + light + shade + unitsWide, () => MakeCloud(seed, light, shade, unitsWide));
    }

    static Sprite MakeCloud(int seed, Color light, Color shade, float unitsWide)
    {
        const int w = 40, h = 20;
        var rnd = new System.Random(seed);
        int n = 4 + rnd.Next(3);
        var cx = new float[n]; var cy = new float[n]; var cr = new float[n];
        for (int i = 0; i < n; i++)
        {
            cx[i] = 7 + (w - 14) * (i + 0.5f) / n + rnd.Next(-2, 3);
            cr[i] = 4 + rnd.Next(5);
            cy[i] = 3 + cr[i] * 0.6f;
        }
        var px = Fill(w, h, (x, y) =>
        {
            if (y < 2) return Color.clear;
            bool inside = false; float top = 0f;
            for (int i = 0; i < n; i++)
            {
                float dx = x + 0.5f - cx[i], dy = y + 0.5f - cy[i];
                if (dx * dx + dy * dy <= cr[i] * cr[i]) { inside = true; top = Mathf.Max(top, cy[i]); }
            }
            if (!inside) return Color.clear;
            return y < top - 1 ? shade : light;
        });
        return FromPixels(px, w, h, w / unitsWide, FilterMode.Point);
    }

    // Cordillera que se repite sin cortes (las ondas usan frecuencias enteras).
    public static Sprite Ridge(int seed, Color body, Color edge, float unitsWide, float unitsHigh, float roughness)
    {
        return Cached("ridge" + seed + body + edge + unitsWide + unitsHigh, () => MakeRidge(seed, body, edge, unitsWide, unitsHigh, roughness));
    }

    static Sprite MakeRidge(int seed, Color body, Color edge, float unitsWide, float unitsHigh, float roughness)
    {
        const int w = 160, h = 48;
        var rnd = new System.Random(seed);
        int waves = 4;
        var k = new int[waves]; var a = new float[waves]; var p = new float[waves];
        for (int i = 0; i < waves; i++)
        {
            k[i] = 1 + i * 2 + rnd.Next(2);
            a[i] = (0.35f / (i + 1)) * (i == 0 ? 1f : roughness);
            p[i] = (float)rnd.NextDouble() * 6.28f;
        }
        var px = Fill(w, h, (x, y) =>
        {
            float t = x / (float)w * Mathf.PI * 2f;
            float v = 0.55f;
            for (int i = 0; i < waves; i++) v += a[i] * Mathf.Sin(t * k[i] + p[i]);
            float top = Mathf.Clamp(v, 0.1f, 0.98f) * h;
            if (y > top) return Color.clear;
            return y > top - 2 ? edge : body;
        });
        return FromPixels(px, w, h, w / unitsWide, FilterMode.Point, new Vector2(0.5f, 0f));
    }

    // Silueta de la fábrica flotante: naves, chimeneas y ventanas encendidas.
    public static Sprite Skyline(int seed, Color body, Color window, float unitsWide)
    {
        return Cached("sky" + seed + body + window + unitsWide, () => MakeSkyline(seed, body, window, unitsWide));
    }

    static Sprite MakeSkyline(int seed, Color body, Color window, float unitsWide)
    {
        const int w = 160, h = 64;
        var rnd = new System.Random(seed);
        var height = new int[w];
        var isChimney = new bool[w];
        int x0 = 0;
        while (x0 < w)
        {
            int bw = 10 + rnd.Next(18);
            int bh = 14 + rnd.Next(30);
            for (int x = x0; x < Mathf.Min(w, x0 + bw); x++) height[x] = bh;
            if (rnd.NextDouble() < 0.6)
            {
                int cxp = x0 + 2 + rnd.Next(Mathf.Max(1, bw - 5));
                int ch = bh + 8 + rnd.Next(16);
                for (int x = cxp; x < Mathf.Min(w, cxp + 3); x++) { height[x] = Mathf.Min(h - 1, ch); isChimney[x] = true; }
            }
            x0 += bw + rnd.Next(4);
        }
        var px = Fill(w, h, (x, y) =>
        {
            if (y >= height[x]) return Color.clear;
            if (isChimney[x] && y >= height[x] - 2) return window * 0.8f + body * 0.2f;
            bool win = !isChimney[x] && y > 3 && y < height[x] - 3 && x % 5 >= 2 && x % 5 <= 3 && y % 6 >= 2 && y % 6 <= 3;
            if (win && ((x * 7 + y * 13) % 5) != 0) return window;
            return body;
        });
        return FromPixels(px, w, h, w / unitsWide, FilterMode.Point, new Vector2(0.5f, 0f));
    }

    // Engrane para el escenario de la fábrica.
    public static Sprite Gear(int teeth)
    {
        return Cached("gear" + teeth, () =>
        {
            const int s = 48;
            float c = s / 2f;
            var px = Fill(s, s, (x, y) =>
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                bool tooth = Mathf.Sin(ang * teeth) > 0f;
                float outer = tooth ? 23f : 19f;
                if (d > outer || d < 6f) return Color.clear;
                if (d < 9f) return new Color(0.75f, 0.75f, 0.75f, 1f);
                if (d > 13f && d < 15f) return new Color(0.8f, 0.8f, 0.8f, 1f);
                return Color.white;
            });
            return FromPixels(px, s, s, s, FilterMode.Point);
        });
    }

    // Luna de pixel art con cráteres.
    public static Sprite Moon => Cached("moon", () =>
    {
        const int s = 32;
        float c = s / 2f;
        var light = new Color(1f, 0.97f, 0.82f);
        var dark = new Color(0.86f, 0.82f, 0.66f);
        var px = Fill(s, s, (x, y) =>
        {
            float dx = x + 0.5f - c, dy = y + 0.5f - c;
            if (dx * dx + dy * dy > c * c) return Color.clear;
            if (Crater(x, y, 10, 20, 4) || Crater(x, y, 20, 11, 3) || Crater(x, y, 19, 23, 2)) return dark;
            return light;
        });
        return FromPixels(px, s, s, s, FilterMode.Point);
    });

    static bool Crater(int x, int y, int cx, int cy, int r)
    {
        int dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }

    // Isla flotante con pasto y tierra (escenario del cielo).
    public static Sprite Island(int seed)
    {
        return Cached("island" + seed, () =>
        {
            const int w = 48, h = 28;
            var rnd = new System.Random(seed);
            var grass = new Color(0.36f, 0.75f, 0.33f);
            var grassDark = new Color(0.22f, 0.55f, 0.27f);
            var dirt = new Color(0.62f, 0.42f, 0.28f);
            var dirtDark = new Color(0.45f, 0.29f, 0.2f);
            var depth = new int[w];
            for (int x = 0; x < w; x++)
            {
                float t = Mathf.Abs(x + 0.5f - w / 2f) / (w / 2f);
                depth[x] = Mathf.RoundToInt((1f - t * t) * (h - 6) + rnd.Next(0, 3));
            }
            var px = Fill(w, h, (x, y) =>
            {
                int fromTop = h - 1 - y;
                if (fromTop >= depth[x] + 4) return Color.clear;
                if (fromTop < 2) return grass;
                if (fromTop < 4) return (x % 3 == 0) ? grassDark : grass;
                return ((x + fromTop) % 7 == 0) ? dirtDark : dirt;
            });
            return FromPixels(px, w, h, 16, FilterMode.Point);
        });
    }

    // Franja de advertencia amarilla y negra (suelo de la fábrica).
    public static Sprite Hazard => Cached("hazard", () =>
    {
        const int w = 16, h = 4;
        var y1 = new Color(1f, 0.8f, 0.2f);
        var k = new Color(0.1f, 0.1f, 0.14f);
        var px = Fill(w, h, (x, y) => ((x + y) / 4) % 2 == 0 ? y1 : k);
        return FromPixels(px, w, h, 16, FilterMode.Point);
    });

    // Cuerpo de tubería: 24 columnas sombreadas (borde, sombra, base, brillo).
    public static Sprite PipeBody(Color dark, Color mid, Color light)
    {
        string key = "pipe" + dark + mid + light;
        return Cached(key, () =>
        {
            const int w = 24;
            var outline = new Color(0.1f, 0.11f, 0.17f);
            var px = Fill(w, 1, (x, y) =>
            {
                if (x == 0 || x == w - 1) return outline;
                if (x < 4) return dark;
                if (x >= 6 && x < 9) return light;
                if (x == 10) return Color.Lerp(mid, light, 0.5f);
                if (x >= w - 6) return dark;
                return mid;
            });
            return FromPixels(px, w, 1, w, FilterMode.Point);
        });
    }

    // Boca de la tubería: más ancha y con borde.
    public static Sprite PipeCap(Color dark, Color mid, Color light)
    {
        string key = "cap" + dark + mid + light;
        return Cached(key, () =>
        {
            const int w = 28, h = 10;
            var outline = new Color(0.1f, 0.11f, 0.17f);
            var px = Fill(w, h, (x, y) =>
            {
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1) return outline;
                if (y == 1) return dark;
                if (y == h - 2) return light;
                if (x < 4) return dark;
                if (x >= 6 && x < 10) return light;
                if (x >= w - 6) return dark;
                return mid;
            });
            return FromPixels(px, w, h, w, FilterMode.Point);
        });
    }

    // Grietas para las tuberías agrietadas.
    public static Sprite Crack => Cached("crack", () =>
    {
        const int s = 24;
        var k = new Color(0.12f, 0.08f, 0.06f);
        var px = Fill(s, s, (x, y) => Color.clear);
        var rnd = new System.Random(7);
        for (int line = 0; line < 3; line++)
        {
            int cx = 6 + rnd.Next(12), cy = 2;
            while (cy < s - 2)
            {
                px[cy * s + Mathf.Clamp(cx, 1, s - 2)] = k;
                cy++;
                cx += rnd.Next(-1, 2);
                if (rnd.NextDouble() < 0.15)
                {
                    int bx = cx;
                    for (int b = 0; b < 4; b++) { bx += line % 2 == 0 ? 1 : -1; px[cy * s + Mathf.Clamp(bx, 1, s - 2)] = k; }
                }
            }
        }
        return FromPixels(px, s, s, s, FilterMode.Point);
    });
}
