using System.Collections.Generic;
using UnityEngine;

// Decodifica las imágenes de los personajes "cabeza" (fotos recortadas).
// Cada imagen se guarda como píxeles RGBA de 128x128 en Base64 dentro de un script,
// para no depender de importar archivos en Unity.
public static class HeadArt
{
    const int Size = 128;
    public const float UnitsWide = 1.4f;

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Decode(string key, string[] data)
    {
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var bytes = System.Convert.FromBase64String(string.Concat(data));
        var px = new Color32[Size * Size];
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color32(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2], bytes[i * 4 + 3]);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size / UnitsWide);
        Cache[key] = sprite;
        return sprite;
    }
}
