using System.Collections.Generic;
using UnityEngine;

public enum ZoneId { Cielo = 0, Fabrica = 1, Tormenta = 2 }

// Datos visuales de cada escenario.
public class ZoneTheme
{
    public ZoneId id;
    public string name;
    public Color skyTop, skyBottom;
    public Color pipeDark, pipeMid, pipeLight;
    public Color ground, groundEdge, ceiling, ceilingEdge;
    public Color uiAccent;
    public int song;

    public static readonly ZoneTheme[] All =
    {
        new ZoneTheme
        {
            id = ZoneId.Cielo, name = "Cielo de las Tuberías", song = 1,
            skyTop = Hex(0x3d8ee8), skyBottom = Hex(0xb8e6ff),
            pipeDark = Hex(0x1f7a3a), pipeMid = Hex(0x3dbb55), pipeLight = Hex(0x9be88a),
            ground = Hex(0xdfeaf7), groundEdge = Hex(0xffffff), ceiling = Hex(0xf2f7ff), ceilingEdge = Hex(0xc8dcf0),
            uiAccent = Hex(0xffd23f),
        },
        new ZoneTheme
        {
            id = ZoneId.Fabrica, name = "Fábrica Flotante", song = 2,
            skyTop = Hex(0x35204f), skyBottom = Hex(0xff9150),
            pipeDark = Hex(0x7a4520), pipeMid = Hex(0xc27a3a), pipeLight = Hex(0xf5c27e),
            ground = Hex(0x2e2a3a), groundEdge = Hex(0x4a4560), ceiling = Hex(0x2e2a3a), ceilingEdge = Hex(0x4a4560),
            uiAccent = Hex(0xffa040),
        },
        new ZoneTheme
        {
            id = ZoneId.Tormenta, name = "Tormenta Nocturna", song = 3,
            skyTop = Hex(0x070b24), skyBottom = Hex(0x2b2f6b),
            pipeDark = Hex(0x1d4a6b), pipeMid = Hex(0x2f8db3), pipeLight = Hex(0x8de6ff),
            ground = Hex(0x20234a), groundEdge = Hex(0x353a6e), ceiling = Hex(0x2a2d5a), ceilingEdge = Hex(0x3c4180),
            uiAccent = Hex(0x8de6ff),
        },
    };

    public static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}

// Construye y anima el fondo de cada escenario: cielo degradado, capas en parallax,
// elementos animados (engranes, humo, estrellas, lluvia, relámpagos), suelo y techo.
public class Scenery
{
    const float Span = 48f;

    class Item
    {
        public Transform t;
        public SpriteRenderer sr;
        public float factor;      // velocidad relativa al escenario (parallax)
        public float spin;        // grados por segundo
        public Vector2 drift;     // movimiento propio
        public float twinkle;     // parpadeo (estrellas)
        public float baseAlpha;
        public bool screenSpace;  // se mueve con la pantalla (lluvia)
        public float wrapTop;     // para elementos que suben o bajan
        public Vector3 baseScale;
        public float lastWobble;
        public float seed;
    }

    readonly Transform parent;
    readonly Camera cam;
    Transform root;
    SpriteRenderer sky;
    readonly List<Item> items = new List<Item>();
    float lightningTimer;

    // Alucinación (cigarro): 0 = normal, 1 = el fondo se derrite, se estira y cambia de color
    public float Trip { get; set; }

    // Laguna (papitas)
    public bool Lagoon { get; set; }
    Transform lagoonRoot;
    readonly List<Item> lagoonItems = new List<Item>();
    readonly List<SpriteRenderer> lagoonRenderers = new List<SpriteRenderer>();
    readonly List<float> lagoonAlpha = new List<float>();
    float lagoonT;

    public ZoneTheme Theme { get; private set; }
    public float Flash { get; private set; }   // destello de relámpago (0..1)
    public event System.Action Thunder;

    public Scenery(Transform parent, Camera cam)
    {
        this.parent = parent;
        this.cam = cam;
    }

    public void Build(ZoneId id)
    {
        if (root != null) Object.Destroy(root.gameObject);
        items.Clear();
        Flash = 0f;
        Theme = ZoneTheme.All[(int)id];
        root = new GameObject("Escenario_" + Theme.name).transform;
        root.SetParent(parent, false);

        sky = SpriteFactory.Shape("Cielo", root, SpriteFactory.Gradient(Theme.skyTop, Theme.skyBottom), Color.white, Vector2.zero, Vector2.one, -100);

        switch (id)
        {
            case ZoneId.Cielo: BuildCielo(); break;
            case ZoneId.Fabrica: BuildFabrica(); break;
            case ZoneId.Tormenta: BuildTormenta(); break;
        }
        BuildGroundAndCeiling();
        lightningTimer = Random.Range(4f, 8f);
    }

    // ------------------------------------------------------------ Escenario 1

    void BuildCielo()
    {
        // Sol con brillo
        Add(SpriteFactory.Shape("BrilloSol", root, SpriteFactory.SoftCircle, new Color(1f, 1f, 0.8f, 0.6f), new Vector2(4f, 3f), new Vector2(5f, 5f), -95), 0.02f);
        Add(SpriteFactory.Shape("Sol", root, SpriteFactory.Circle, new Color(1f, 0.97f, 0.75f), new Vector2(4f, 3f), new Vector2(1.6f, 1.6f), -94), 0.02f);

        // Montañas lejanas y cercanas
        RidgeLayer(101, ZoneTheme.Hex(0xa9cdee), ZoneTheme.Hex(0xd6ecff), 0.08f, 5.5f, -90, 0.6f);
        RidgeLayer(202, ZoneTheme.Hex(0x86b7e0), ZoneTheme.Hex(0xb6dbf7), 0.15f, 3.8f, -85, 1f);

        // Nubes lejanas
        for (int i = 0; i < 7; i++)
        {
            var c = SpriteFactory.PixelCloud(i, Color.white, ZoneTheme.Hex(0xdbe9f7), 3.5f);
            Add(SpriteFactory.Shape("NubeLejana", root, c, new Color(1f, 1f, 1f, 0.85f), new Vector2(X(i, 7), Random.Range(1.2f, 4f)), Vector2.one * Random.Range(0.8f, 1.2f), -80), 0.22f);
        }
        // Islas flotantes con tuberías antiguas
        for (int i = 0; i < 4; i++)
        {
            var isl = SpriteFactory.Shape("Isla", root, SpriteFactory.Island(i), Color.white, new Vector2(X(i, 4), Random.Range(-1.5f, 2f)), Vector2.one * Random.Range(0.8f, 1.3f), -70);
            var pipe = SpriteFactory.Shape("TuboViejo", isl.transform, SpriteFactory.PipeBody(ZoneTheme.Hex(0x4a7a52), ZoneTheme.Hex(0x6aa070), ZoneTheme.Hex(0xa8d4a0)), new Color(1f, 1f, 1f, 0.9f), new Vector2(Random.Range(-0.8f, 0.8f), 1.2f), new Vector2(0.45f, 1.4f * 24f), -71);
            pipe.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));
            Add(isl, 0.35f);
        }
        // Nubes cercanas
        for (int i = 0; i < 5; i++)
        {
            var c = SpriteFactory.PixelCloud(20 + i, Color.white, ZoneTheme.Hex(0xc9dcf0), 5f);
            Add(SpriteFactory.Shape("NubeCercana", root, c, Color.white, new Vector2(X(i, 5), Random.Range(-3.3f, -1.8f)), Vector2.one, -60), 0.6f);
        }
        // Pájaros lejanos (puntos que planean)
        for (int i = 0; i < 4; i++)
        {
            var b = SpriteFactory.Shape("Ave", root, SpriteFactory.Square, new Color(0.2f, 0.3f, 0.45f, 0.6f), new Vector2(X(i, 4), Random.Range(2f, 4f)), new Vector2(0.18f, 0.06f), -82);
            var it = Add(b, 0.3f);
            it.drift = new Vector2(-0.3f, 0f);
        }
    }

    // ------------------------------------------------------------ Escenario 2

    void BuildFabrica()
    {
        // Sol grande del atardecer
        Add(SpriteFactory.Shape("BrilloSol", root, SpriteFactory.SoftCircle, new Color(1f, 0.6f, 0.3f, 0.7f), new Vector2(-2f, -0.5f), new Vector2(9f, 9f), -95), 0.02f);
        Add(SpriteFactory.Shape("Sol", root, SpriteFactory.Circle, ZoneTheme.Hex(0xffcf6e), new Vector2(-2f, -0.5f), new Vector2(3f, 3f), -94), 0.02f);

        // Siluetas de la fábrica flotante, dos capas
        for (int i = 0; i < 3; i++)
        {
            var s = SpriteFactory.Skyline(300 + i, ZoneTheme.Hex(0x6b3d6b), ZoneTheme.Hex(0xffb35c), Span / 3f);
            Add(SpriteFactory.Shape("FabricaLejana", root, s, Color.white, new Vector2(-Span / 2f + Span / 3f * (i + 0.5f), GameConfig.GroundY - 0.3f), Vector2.one, -88), 0.1f);
        }
        for (int i = 0; i < 3; i++)
        {
            var s = SpriteFactory.Skyline(400 + i, ZoneTheme.Hex(0x3e2448), ZoneTheme.Hex(0xffd36e), Span / 3f);
            Add(SpriteFactory.Shape("FabricaCercana", root, s, Color.white, new Vector2(-Span / 2f + Span / 3f * (i + 0.5f), GameConfig.GroundY - 0.3f), new Vector2(1f, 0.75f), -84), 0.2f);
        }

        // Engranes girando
        for (int i = 0; i < 6; i++)
        {
            float size = Random.Range(1.2f, 2.6f);
            var g = SpriteFactory.Shape("Engrane", root, SpriteFactory.Gear(8 + i % 3 * 2), new Color(0.55f, 0.36f, 0.5f, 0.85f), new Vector2(X(i, 6), Random.Range(-2.5f, 3f)), Vector2.one * size, -75);
            var it = Add(g, 0.35f);
            it.spin = (i % 2 == 0 ? 40f : -40f) / size;
        }

        // Humo que sube
        for (int i = 0; i < 10; i++)
        {
            var sm = SpriteFactory.Shape("Humo", root, SpriteFactory.SoftCircle, new Color(0.5f, 0.42f, 0.5f, 0.35f), new Vector2(X(i, 10), Random.Range(-3f, 4f)), Vector2.one * Random.Range(1.5f, 3f), -78);
            var it = Add(sm, 0.15f);
            it.drift = new Vector2(0.1f, 0.4f);
            it.wrapTop = 6f;
        }

        // Chispas flotantes
        for (int i = 0; i < 16; i++)
        {
            var e = SpriteFactory.Shape("Chispa", root, SpriteFactory.Square, ZoneTheme.Hex(0xffb347), new Vector2(X(i, 16), Random.Range(-4f, 4f)), Vector2.one * 0.08f, -50);
            var it = Add(e, 0.7f);
            it.drift = new Vector2(0f, Random.Range(0.3f, 0.8f));
            it.wrapTop = 5f;
            it.twinkle = Random.Range(3f, 7f);
        }
    }

    // ------------------------------------------------------------ Escenario 3

    void BuildTormenta()
    {
        // Estrellas
        for (int i = 0; i < 50; i++)
        {
            var s = SpriteFactory.Shape("Estrella", root, SpriteFactory.Square, Color.white, new Vector2(Random.Range(-Span / 2f, Span / 2f), Random.Range(-1f, 5f)), Vector2.one * Random.Range(0.04f, 0.1f), -98);
            var it = Add(s, 0.01f);
            it.twinkle = Random.Range(1f, 4f);
        }
        // Luna
        Add(SpriteFactory.Shape("BrilloLuna", root, SpriteFactory.SoftCircle, new Color(0.7f, 0.75f, 1f, 0.4f), new Vector2(3.5f, 3f), new Vector2(5f, 5f), -95), 0.01f);
        Add(SpriteFactory.Shape("Luna", root, SpriteFactory.Moon, Color.white, new Vector2(3.5f, 3f), Vector2.one * 1.8f, -94), 0.01f);

        // Montañas oscuras
        RidgeLayer(505, ZoneTheme.Hex(0x161a40), ZoneTheme.Hex(0x4a52a0), 0.08f, 5f, -90, 1.2f);
        RidgeLayer(606, ZoneTheme.Hex(0x0c0e26), ZoneTheme.Hex(0x343a78), 0.16f, 3.5f, -86, 1f);

        // Nubes de tormenta
        for (int i = 0; i < 7; i++)
        {
            var c = SpriteFactory.PixelCloud(50 + i, ZoneTheme.Hex(0x4a4f80), ZoneTheme.Hex(0x30345e), 5f);
            Add(SpriteFactory.Shape("NubeTormenta", root, c, new Color(1f, 1f, 1f, 0.9f), new Vector2(X(i, 7), Random.Range(2f, 4.2f)), Vector2.one * Random.Range(1f, 1.4f), -80), 0.25f);
        }
        for (int i = 0; i < 4; i++)
        {
            var c = SpriteFactory.PixelCloud(70 + i, ZoneTheme.Hex(0x3a3e6c), ZoneTheme.Hex(0x262a52), 6f);
            Add(SpriteFactory.Shape("NubeBaja", root, c, Color.white, new Vector2(X(i, 4), Random.Range(-3.5f, -2.5f)), Vector2.one, -60), 0.55f);
        }

        // Lluvia (se mueve con la pantalla)
        for (int i = 0; i < 60; i++)
        {
            var r = SpriteFactory.Shape("Gota", root, SpriteFactory.Square, new Color(0.6f, 0.7f, 1f, 0.35f), new Vector2(Random.Range(-12f, 12f), Random.Range(-5f, 6f)), new Vector2(0.03f, 0.35f), 30);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            var it = Add(r, 0f);
            it.screenSpace = true;
            it.drift = new Vector2(-2.5f, -11f);
            it.wrapTop = 6f;
        }
    }

    // ------------------------------------------------------------ Suelo y techo

    void BuildGroundAndCeiling()
    {
        var t = Theme;
        SpriteFactory.Shape("Suelo", root, SpriteFactory.Square, t.ground,
            new Vector2(0f, (GameConfig.GroundY - 8f) * 0.5f), new Vector2(120f, GameConfig.GroundY + 8f), 20);
        SpriteFactory.Shape("Techo", root, SpriteFactory.Square, t.ceiling,
            new Vector2(0f, (GameConfig.CeilingY + 8f) * 0.5f), new Vector2(120f, 8f - GameConfig.CeilingY), 20);

        if (t.id == ZoneId.Fabrica)
        {
            // Vigas metálicas con franja de advertencia
            for (int i = 0; i < 48; i++)
            {
                Add(SpriteFactory.Shape("Franja", root, SpriteFactory.Hazard, Color.white, new Vector2(-Span / 2f + i, GameConfig.GroundY - 0.12f), Vector2.one, 21), 1f);
                Add(SpriteFactory.Shape("FranjaTecho", root, SpriteFactory.Hazard, Color.white, new Vector2(-Span / 2f + i, GameConfig.CeilingY + 0.12f), Vector2.one, 21), 1f);
                if (i % 3 == 0)
                {
                    Add(SpriteFactory.Shape("Remache", root, SpriteFactory.Square, t.groundEdge, new Vector2(-Span / 2f + i, GameConfig.GroundY - 0.6f), Vector2.one * 0.15f, 21), 1f);
                    Add(SpriteFactory.Shape("RemacheTecho", root, SpriteFactory.Square, t.ceilingEdge, new Vector2(-Span / 2f + i, GameConfig.CeilingY + 0.6f), Vector2.one * 0.15f, 21), 1f);
                }
            }
            return;
        }

        // Bordes de nubes esponjosas
        for (int i = 0; i < 40; i++)
        {
            float x = -Span / 2f + (i + 0.5f) * Span / 40f;
            Add(SpriteFactory.Shape("BordeSuelo", root, SpriteFactory.PixelCloud(90 + i % 4, t.groundEdge, t.ground, 2.4f), Color.white, new Vector2(x, GameConfig.GroundY - 0.35f), Vector2.one, 21), 1f);
            var top = SpriteFactory.Shape("BordeTecho", root, SpriteFactory.PixelCloud(95 + i % 4, t.ceiling, t.ceilingEdge, 2.4f), Color.white, new Vector2(x, GameConfig.CeilingY + 0.35f), Vector2.one, 21);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Add(top, 1f);
        }
    }

    // ------------------------------------------------------------ Utilidades

    void RidgeLayer(int seed, Color body, Color edge, float factor, float height, int order, float rough)
    {
        for (int i = 0; i < 2; i++)
        {
            var s = SpriteFactory.Ridge(seed, body, edge, Span / 2f, height, rough);
            float naturalHeight = s.bounds.size.y;
            Add(SpriteFactory.Shape("Montanas", root, s, Color.white, new Vector2(-Span / 4f + Span / 2f * i, GameConfig.GroundY - 0.5f), new Vector2(1f, height / naturalHeight), order), factor);
        }
    }

    Item Add(SpriteRenderer sr, float factor)
    {
        var it = new Item { t = sr.transform, sr = sr, factor = factor, baseAlpha = sr.color.a, baseScale = sr.transform.localScale, seed = Random.value * 10f };
        items.Add(it);
        return it;
    }

    static float X(int i, int count)
    {
        return -Span * 0.5f + (i + Random.Range(0.15f, 0.85f)) * Span / count;
    }

    public void Tick(float dt, float dx, bool playing)
    {
        if (root == null) return;
        float halfW = cam.orthographicSize * cam.aspect;
        sky.transform.localScale = new Vector3(halfW * 2f + 2f, cam.orthographicSize * 2f + 2f, 1f);

        foreach (var it in items)
        {
            var p = it.t.localPosition;
            if (!it.screenSpace) p.x -= dx * it.factor;
            p += (Vector3)(it.drift * dt);
            if (it.screenSpace)
            {
                if (p.y < -6f) { p.y += 12f; p.x = Random.Range(-halfW - 2f, halfW + 3f); }
                if (p.x < -halfW - 3f) p.x += halfW * 2f + 6f;
            }
            else
            {
                if (p.x < -Span * 0.5f) p.x += Span;
                if (it.wrapTop > 0f && p.y > it.wrapTop) p.y -= it.wrapTop * 2f;
            }
            // Alucinación: ondula, se derrite y se estira
            if (!it.screenSpace && (Trip > 0f || it.lastWobble != 0f))
            {
                float wob = Mathf.Sin(Time.time * 2.3f + it.seed + p.x * 0.4f) * 0.6f * Trip;
                p.y += wob - it.lastWobble;
                it.lastWobble = wob;
                var bs = it.baseScale;
                it.t.localScale = new Vector3(
                    bs.x * (1f + 0.25f * Mathf.Sin(Time.time * 1.7f + it.seed) * Trip),
                    bs.y * (1f + 0.5f * Mathf.Sin(Time.time * 1.3f + it.seed * 2f) * Trip), 1f);
            }
            it.t.localPosition = p;
            if (it.spin != 0f) it.t.Rotate(0f, 0f, it.spin * dt * (1f + Trip * 4f));
            if (it.twinkle > 0f)
            {
                var c = it.sr.color;
                c.a = it.baseAlpha * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * it.twinkle + p.x)));
                it.sr.color = c;
            }
        }

        // Cielo de colores durante la alucinación
        if (Trip > 0f)
        {
            var hue = Color.HSVToRGB((Time.time * 0.2f) % 1f, 0.65f, 1f);
            sky.color = Color.Lerp(Color.white, hue, Trip * 0.8f);
        }
        else sky.color = Color.white;

        TickLagoon(dt, dx);

        // Relámpagos en la tormenta
        Flash = Mathf.MoveTowards(Flash, 0f, dt * 2.5f);
        if (Theme.id == ZoneId.Tormenta)
        {
            lightningTimer -= dt;
            if (lightningTimer <= 0f)
            {
                lightningTimer = Random.Range(6f, 12f);
                Flash = 1f;
                Thunder?.Invoke();
            }
        }
    }

    // ------------------------------------------------------------ Laguna (papitas)

    void BuildLagoon()
    {
        lagoonRoot = new GameObject("Laguna").transform;
        lagoonRoot.SetParent(parent, false);
        float top = GameConfig.GroundY + 2.3f;
        var light = new Color(0.45f, 0.8f, 1f, 1f);
        LagoonShape(SpriteFactory.Shape("Agua", lagoonRoot, SpriteFactory.Gradient(new Color(0.25f, 0.65f, 0.95f), new Color(0.08f, 0.3f, 0.6f)), Color.white,
            new Vector2(0f, (top + GameConfig.GroundY - 1f) * 0.5f), new Vector2(120f, top - GameConfig.GroundY + 1f), 19), 0.95f, 0f);
        LagoonShape(SpriteFactory.Shape("Orilla", lagoonRoot, SpriteFactory.Square, light, new Vector2(0f, top), new Vector2(120f, 0.08f), 19), 1f, 0f);
        for (int i = 0; i < 40; i++)
        {
            float x = -Span / 2f + i * Span / 40f;
            var w = SpriteFactory.Shape("Ola", lagoonRoot, SpriteFactory.Square, light, new Vector2(x, Random.Range(GameConfig.GroundY + 0.2f, top - 0.2f)), new Vector2(Random.Range(0.3f, 0.8f), 0.05f), 19);
            var it = LagoonShape(w, 0.7f, 1f);
            it.twinkle = Random.Range(1.5f, 4f);
        }
        for (int i = 0; i < 9; i++)
        {
            float x = -Span / 2f + (i + Random.value) * Span / 9f;
            float y = Random.Range(GameConfig.GroundY + 0.5f, top - 0.3f);
            var pad = SpriteFactory.Shape("Lirio", lagoonRoot, SpriteFactory.Circle, ZoneTheme.Hex(0x38b764), new Vector2(x, y), new Vector2(0.7f, 0.22f), 19);
            LagoonShape(pad, 1f, 1f);
            if (i % 2 == 0)
                LagoonShape(SpriteFactory.Shape("Flor", lagoonRoot, SpriteFactory.Circle, ZoneTheme.Hex(0xff8fb1), new Vector2(x + 0.1f, y + 0.08f), new Vector2(0.18f, 0.14f), 19), 1f, 1f);
        }
    }

    Item LagoonShape(SpriteRenderer sr, float alpha, float factor)
    {
        var it = new Item { t = sr.transform, sr = sr, factor = factor, baseAlpha = alpha };
        lagoonItems.Add(it);
        lagoonRenderers.Add(sr);
        lagoonAlpha.Add(alpha);
        return it;
    }

    void TickLagoon(float dt, float dx)
    {
        lagoonT = Mathf.MoveTowards(lagoonT, Lagoon ? 1f : 0f, dt * 1.5f);
        if (lagoonRoot == null)
        {
            if (lagoonT <= 0f) return;
            BuildLagoon();
        }
        lagoonRoot.gameObject.SetActive(lagoonT > 0f);
        if (lagoonT <= 0f) return;
        // La laguna sube desde el suelo
        lagoonRoot.localPosition = new Vector3(0f, -(1f - lagoonT) * 2.5f, 0f);
        for (int i = 0; i < lagoonItems.Count; i++)
        {
            var it = lagoonItems[i];
            var p = it.t.localPosition;
            p.x -= dx * it.factor;
            if (it.factor > 0f && p.x < -Span * 0.5f) p.x += Span;
            it.t.localPosition = p;
            float a = lagoonAlpha[i] * lagoonT;
            if (it.twinkle > 0f) a *= 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * it.twinkle + p.x));
            var c = lagoonRenderers[i].color;
            c.a = a;
            lagoonRenderers[i].color = c;
        }
    }
}
