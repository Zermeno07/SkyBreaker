using UnityEngine;

public enum PipeKind
{
    Normal,  // no se puede romper ni traspasar sin un poder
    Cracked, // agrietada: se rompe con un disparo
    Ghost    // fantasma: azul translúcida, se puede traspasar
}

// Par de tuberías (arriba y abajo) con un espacio para pasar. Se reutiliza mediante pooling.
public class PipePair : MonoBehaviour
{
    const float PipeLength = 14f;
    const float CapHeight = 0.45f;
    const float CapExtra = 0.3f;

    class Half
    {
        public Transform root;
        public SpriteRenderer body, cap;
        public SpriteRenderer[] cracks;
        public bool alive;
    }

    Half top, bottom;
    bool faded;

    public PipeKind Kind { get; private set; }
    public float GapCenter { get; private set; }
    public float GapSize { get; private set; }
    public bool Scored { get; set; }
    public float X => transform.position.x;
    public float HalfWidth => (GameConfig.PipeWidth + CapExtra) * 0.5f;
    public bool IsSolid => Kind != PipeKind.Ghost;

    public static PipePair Create(Transform parent)
    {
        var go = new GameObject("Tuberias");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PipePair>();
    }

    void Awake()
    {
        top = MakeHalf("Arriba", true);
        bottom = MakeHalf("Abajo", false);
    }

    Half MakeHalf(string name, bool isTop)
    {
        var h = new Half { root = new GameObject(name).transform };
        h.root.SetParent(transform, false);
        float dir = isTop ? 1f : -1f;
        h.body = SpriteFactory.Shape("Cuerpo", h.root, SpriteFactory.Square, Color.white,
            new Vector2(0f, dir * PipeLength * 0.5f), new Vector2(GameConfig.PipeWidth, PipeLength * 24f), 0);
        h.cap = SpriteFactory.Shape("Boca", h.root, SpriteFactory.Square, Color.white,
            new Vector2(0f, dir * CapHeight * 0.5f), new Vector2(GameConfig.PipeWidth + CapExtra, CapHeight * 28f / 10f), 2);
        h.cracks = new SpriteRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            h.cracks[i] = SpriteFactory.Shape("Grieta", h.root, SpriteFactory.Crack, Color.white,
                new Vector2(i == 0 ? -0.15f : 0.2f, dir * (0.9f + i * 1.3f)), Vector2.one * 0.9f, 1);
            if (i == 1) h.cracks[i].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        return h;
    }

    public void Setup(float x, float gapCenter, float gapSize, PipeKind kind, ZoneTheme zone)
    {
        transform.position = new Vector3(x, 0f, 0f);
        GapCenter = gapCenter;
        GapSize = gapSize;
        Kind = kind;
        Scored = false;
        top.root.localPosition = new Vector3(0f, gapCenter + gapSize * 0.5f, 0f);
        bottom.root.localPosition = new Vector3(0f, gapCenter - gapSize * 0.5f, 0f);
        top.alive = bottom.alive = true;
        top.root.gameObject.SetActive(true);
        bottom.root.gameObject.SetActive(true);
        faded = false;
        ApplyTheme(zone);
        gameObject.SetActive(true);
    }

    public void ApplyTheme(ZoneTheme zone)
    {
        Color dark = zone.pipeDark, mid = zone.pipeMid, light = zone.pipeLight;
        if (Kind == PipeKind.Cracked)
        {
            dark = ZoneTheme.Hex(0x6b2e14); mid = ZoneTheme.Hex(0xa8552a); light = ZoneTheme.Hex(0xe0915a);
        }
        else if (Kind == PipeKind.Ghost)
        {
            dark = ZoneTheme.Hex(0x3a5fd0); mid = ZoneTheme.Hex(0x6f9bff); light = ZoneTheme.Hex(0xc8e0ff);
        }
        var bodySprite = SpriteFactory.PipeBody(dark, mid, light);
        var capSprite = SpriteFactory.PipeCap(dark, mid, light);
        foreach (var h in new[] { top, bottom })
        {
            h.body.sprite = bodySprite;
            h.cap.sprite = capSprite;
            foreach (var c in h.cracks) c.enabled = Kind == PipeKind.Cracked;
        }
        RefreshAlpha();
    }

    // Con el poder fantasma todas las tuberías se ven translúcidas.
    public void SetFaded(bool value)
    {
        if (faded == value) return;
        faded = value;
        RefreshAlpha();
    }

    void RefreshAlpha()
    {
        float a = Kind == PipeKind.Ghost ? 0.5f : 1f;
        if (faded) a = Mathf.Min(a, 0.35f);
        foreach (var h in new[] { top, bottom })
        {
            h.body.color = new Color(1f, 1f, 1f, a);
            h.cap.color = new Color(1f, 1f, 1f, a);
        }
    }

    public void Tick(float dx)
    {
        transform.position += new Vector3(-dx, 0f, 0f);
        if (Kind == PipeKind.Ghost && !faded)
        {
            float a = 0.4f + 0.15f * Mathf.Sin(Time.time * 4f + X);
            top.body.color = top.cap.color = bottom.body.color = bottom.cap.color = new Color(1f, 1f, 1f, a);
        }
    }

    float GapTop => GapCenter + GapSize * 0.5f;
    float GapBottom => GapCenter - GapSize * 0.5f;

    public bool Overlaps(Vector2 c, float r)
    {
        float x = X;
        float hw = HalfWidth;
        if (Mathf.Abs(c.x - x) > hw + r) return false;
        return (top.alive && GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, GapTop, 100f))
            || (bottom.alive && GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, -100f, GapBottom));
    }

    // Un disparo choca con una mitad: devuelve 0 si no choca, 1 si choca y la mitad resiste,
    // 2 si la mitad estaba agrietada y se rompió.
    public int HitByShot(Vector2 c, float r, out Vector2 hitPoint)
    {
        hitPoint = c;
        if (!IsSolid) return 0;
        float x = X, hw = HalfWidth;
        if (Mathf.Abs(c.x - x) > hw + r) return 0;
        Half h = null;
        if (top.alive && GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, GapTop, 100f)) h = top;
        else if (bottom.alive && GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, -100f, GapBottom)) h = bottom;
        if (h == null) return 0;
        hitPoint = new Vector2(x - hw, c.y);
        if (Kind != PipeKind.Cracked) return 1;
        h.alive = false;
        h.root.gameObject.SetActive(false);
        return 2;
    }
}
