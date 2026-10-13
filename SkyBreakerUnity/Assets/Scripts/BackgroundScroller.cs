using System.Collections.Generic;
using UnityEngine;

// Fondo en parallax de tres capas (nubes lejanas, ruinas de la fábrica flotante, nubes cercanas),
// más el suelo y el techo de nubes.
public class BackgroundScroller
{
    const float Span = 44f;

    class Layer
    {
        public float factor;
        public readonly List<Transform> items = new List<Transform>();
    }

    readonly List<Layer> layers = new List<Layer>();

    public BackgroundScroller(Transform parent)
    {
        var root = new GameObject("Fondo").transform;
        root.SetParent(parent, false);

        // Capa 1: nubes lejanas
        var far = AddLayer(0.1f);
        for (int i = 0; i < 8; i++)
            far.items.Add(Cloud(root, X(i, 8), Random.Range(1f, 4f), Random.Range(1.2f, 2.2f), new Color(1f, 1f, 1f, 0.45f), -30));

        // Capa 2: ruinas de la fábrica flotante
        var ruins = AddLayer(0.3f);
        var ruinColor = new Color(0.5f, 0.45f, 0.65f, 0.55f);
        for (int i = 0; i < 12; i++)
        {
            float w = Random.Range(0.8f, 2.2f);
            float h = Random.Range(1.5f, 4.5f);
            var r = SpriteFactory.Shape("Ruina", root, false, ruinColor,
                new Vector2(X(i, 12), GameConfig.GroundY + h * 0.5f - 0.3f), new Vector2(w, h), -20);
            ruins.items.Add(r.transform);
        }

        // Capa 3: nubes cercanas
        var near = AddLayer(0.6f);
        for (int i = 0; i < 6; i++)
            near.items.Add(Cloud(root, X(i, 6), Random.Range(-3f, 3.5f), Random.Range(1.6f, 2.6f), new Color(1f, 1f, 1f, 0.75f), -10));

        // Suelo: franja de nubes oscuras
        var groundColor = new Color(0.27f, 0.29f, 0.42f);
        SpriteFactory.Shape("Suelo", root, false, groundColor,
            new Vector2(0f, (GameConfig.GroundY - 8f) * 0.5f), new Vector2(100f, GameConfig.GroundY + 8f), 20);
        var groundBumps = AddLayer(1f);
        for (int i = 0; i < 55; i++)
            groundBumps.items.Add(SpriteFactory.Shape("NubeSuelo", root, true, groundColor,
                new Vector2(X(i, 55), GameConfig.GroundY), new Vector2(1.1f, 0.7f), 20).transform);

        // Techo: borde de nubes densas
        var ceilingColor = new Color(0.93f, 0.95f, 1f);
        SpriteFactory.Shape("Techo", root, false, ceilingColor,
            new Vector2(0f, (GameConfig.CeilingY + 8f) * 0.5f), new Vector2(100f, 8f - GameConfig.CeilingY), 20);
        var ceilingBumps = AddLayer(1f);
        for (int i = 0; i < 55; i++)
            ceilingBumps.items.Add(SpriteFactory.Shape("NubeTecho", root, true, ceilingColor,
                new Vector2(X(i, 55), GameConfig.CeilingY), new Vector2(1.1f, 0.6f), 20).transform);
    }

    static float X(int i, int count)
    {
        return -Span * 0.5f + (i + Random.Range(0.1f, 0.9f)) * Span / count;
    }

    Layer AddLayer(float factor)
    {
        var l = new Layer { factor = factor };
        layers.Add(l);
        return l;
    }

    static Transform Cloud(Transform parent, float x, float y, float size, Color c, int order)
    {
        var t = new GameObject("Nube").transform;
        t.SetParent(parent, false);
        t.localPosition = new Vector3(x, y, 0f);
        SpriteFactory.Shape("a", t, true, c, new Vector2(-0.35f * size, 0f), new Vector2(size * 0.7f, size * 0.5f), order);
        SpriteFactory.Shape("b", t, true, c, new Vector2(0f, 0.15f * size), new Vector2(size * 0.8f, size * 0.65f), order);
        SpriteFactory.Shape("c", t, true, c, new Vector2(0.35f * size, 0f), new Vector2(size * 0.7f, size * 0.5f), order);
        return t;
    }

    public void Tick(float dx)
    {
        foreach (var layer in layers)
        {
            foreach (var t in layer.items)
            {
                var p = t.localPosition;
                p.x -= dx * layer.factor;
                if (p.x < -Span * 0.5f) p.x += Span;
                t.localPosition = p;
            }
        }
    }
}
