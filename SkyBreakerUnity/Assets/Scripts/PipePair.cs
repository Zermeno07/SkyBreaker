using UnityEngine;

// Par de tuberías (arriba y abajo) con un espacio para pasar. Se reutiliza mediante pooling.
public class PipePair : MonoBehaviour
{
    const float PipeLength = 14f;
    const float CapHeight = 0.4f;
    const float CapExtra = 0.3f;

    Transform top, bottom, topCap, bottomCap, topShine, bottomShine;

    public float GapCenter { get; private set; }
    public float GapSize { get; private set; }
    public bool Scored { get; set; }
    public float X => transform.position.x;
    public float HalfWidth => (GameConfig.PipeWidth + CapExtra) * 0.5f;

    public static PipePair Create(Transform parent)
    {
        var go = new GameObject("Tuberias");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PipePair>();
    }

    void Awake()
    {
        var body = new Color(0.36f, 0.75f, 0.29f);
        var cap = new Color(0.26f, 0.6f, 0.2f);
        var shine = new Color(0.6f, 0.9f, 0.5f);
        var w = GameConfig.PipeWidth;
        top = SpriteFactory.Shape("Arriba", transform, false, body, Vector2.zero, new Vector2(w, PipeLength), 0).transform;
        bottom = SpriteFactory.Shape("Abajo", transform, false, body, Vector2.zero, new Vector2(w, PipeLength), 0).transform;
        topShine = SpriteFactory.Shape("BrilloArriba", transform, false, shine, Vector2.zero, new Vector2(0.15f, PipeLength), 1).transform;
        bottomShine = SpriteFactory.Shape("BrilloAbajo", transform, false, shine, Vector2.zero, new Vector2(0.15f, PipeLength), 1).transform;
        topCap = SpriteFactory.Shape("BordeArriba", transform, false, cap, Vector2.zero, new Vector2(w + CapExtra, CapHeight), 2).transform;
        bottomCap = SpriteFactory.Shape("BordeAbajo", transform, false, cap, Vector2.zero, new Vector2(w + CapExtra, CapHeight), 2).transform;
    }

    public void Setup(float x, float gapCenter, float gapSize)
    {
        transform.position = new Vector3(x, 0f, 0f);
        GapCenter = gapCenter;
        GapSize = gapSize;
        Scored = false;

        float gapTop = gapCenter + gapSize * 0.5f;
        float gapBottom = gapCenter - gapSize * 0.5f;
        float shineX = -GameConfig.PipeWidth * 0.25f;

        top.localPosition = new Vector3(0f, gapTop + PipeLength * 0.5f, 0f);
        topShine.localPosition = new Vector3(shineX, gapTop + PipeLength * 0.5f, 0f);
        topCap.localPosition = new Vector3(0f, gapTop + CapHeight * 0.5f, 0f);

        bottom.localPosition = new Vector3(0f, gapBottom - PipeLength * 0.5f, 0f);
        bottomShine.localPosition = new Vector3(shineX, gapBottom - PipeLength * 0.5f, 0f);
        bottomCap.localPosition = new Vector3(0f, gapBottom - CapHeight * 0.5f, 0f);

        gameObject.SetActive(true);
    }

    public void Move(float dx)
    {
        transform.position += new Vector3(-dx, 0f, 0f);
    }

    public bool Overlaps(Vector2 c, float r)
    {
        float x = X;
        float hw = HalfWidth;
        if (Mathf.Abs(c.x - x) > hw + r) return false;
        float gapTop = GapCenter + GapSize * 0.5f;
        float gapBottom = GapCenter - GapSize * 0.5f;
        return GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, gapTop, 100f)
            || GameConfig.CircleOverlapsRect(c, r, x - hw, x + hw, -100f, gapBottom);
    }
}
