using UnityEngine;

// Genera el arte provisional (círculos y cuadrados) por código, como pide el prototipo.
public static class SpriteFactory
{
    static Sprite square;
    static Sprite circle;

    public static Sprite Square
    {
        get { if (square == null) square = Make(false); return square; }
    }

    public static Sprite Circle
    {
        get { if (circle == null) circle = Make(true); return circle; }
    }

    static Sprite Make(bool round)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                bool inside = !round || dx * dx + dy * dy <= r * r;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        // pixelsPerUnit = size, así el sprite mide 1x1 unidad y se escala con localScale
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static SpriteRenderer Shape(string name, Transform parent, bool round, Color color,
        Vector2 localPos, Vector2 size, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? Circle : Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }
}
