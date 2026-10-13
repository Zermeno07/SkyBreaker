using UnityEngine;

// Pico: aleteo, gravedad, invulnerabilidad tras un golpe y poder de encogerse.
public class PlayerBird : MonoBehaviour
{
    float velocityY;
    float invulnerableTimer;
    float shrinkTimer;
    Transform visual;
    Transform wing;
    SpriteRenderer[] renderers;

    public bool IsInvulnerable => invulnerableTimer > 0f;
    public bool IsShrunk => shrinkTimer > 0f;
    public float ShrinkRemaining01 => shrinkTimer / GameConfig.ShrinkDuration;
    public float Radius => GameConfig.BirdRadius * (IsShrunk ? 0.5f : 1f);
    public Vector2 Position => transform.position;

    public static PlayerBird Create(Transform parent)
    {
        var go = new GameObject("Pico");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PlayerBird>();
    }

    void Awake()
    {
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);

        var blue = new Color(0.2f, 0.55f, 0.95f);
        SpriteFactory.Shape("Cuerpo", visual, true, blue, Vector2.zero, new Vector2(0.78f, 0.72f), 10);
        SpriteFactory.Shape("Pecho", visual, true, new Color(0.85f, 0.93f, 1f), new Vector2(0.06f, -0.12f), new Vector2(0.45f, 0.38f), 11);
        SpriteFactory.Shape("Ojo", visual, true, Color.white, new Vector2(0.16f, 0.13f), new Vector2(0.28f, 0.28f), 12);
        SpriteFactory.Shape("Pupila", visual, true, Color.black, new Vector2(0.21f, 0.13f), new Vector2(0.12f, 0.14f), 13);
        SpriteFactory.Shape("Pico", visual, false, new Color(1f, 0.8f, 0.15f), new Vector2(0.42f, -0.03f), new Vector2(0.22f, 0.13f), 12);
        wing = SpriteFactory.Shape("Ala", visual, true, new Color(0.12f, 0.4f, 0.8f), new Vector2(-0.14f, -0.02f), new Vector2(0.38f, 0.22f), 12).transform;

        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void ResetBird(float x)
    {
        transform.position = new Vector3(x, 0f, 0f);
        transform.rotation = Quaternion.identity;
        velocityY = 0f;
        invulnerableTimer = 0f;
        shrinkTimer = 0f;
        visual.localScale = Vector3.one;
        SetVisible(true);
    }

    public void Flap()
    {
        velocityY = GameConfig.FlapVelocity;
    }

    public void IdleBob(float x)
    {
        transform.position = new Vector3(x, Mathf.Sin(Time.time * 3f) * 0.3f, 0f);
        transform.rotation = Quaternion.identity;
        AnimateWing(10f);
    }

    public void TickPhysics(float dt)
    {
        velocityY = Mathf.Max(velocityY + GameConfig.Gravity * dt, GameConfig.MaxFallSpeed);
        var p = transform.position;
        p.y += velocityY * dt;
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(velocityY * 4f, -70f, 30f));

        if (invulnerableTimer > 0f) invulnerableTimer -= dt;
        if (shrinkTimer > 0f) shrinkTimer -= dt;

        float targetScale = IsShrunk ? 0.5f : 1f;
        float s = Mathf.MoveTowards(visual.localScale.x, targetScale, dt * 3f);
        visual.localScale = new Vector3(s, s, 1f);

        // Parpadeo durante la invulnerabilidad
        SetVisible(!IsInvulnerable || ((int)(Time.time * 15f)) % 2 == 0);
        AnimateWing(velocityY > 0f ? 25f : 8f);
    }

    // Caída tras perder la última vida
    public void TickDead(float dt)
    {
        SetVisible(true);
        var p = transform.position;
        float floor = GameConfig.GroundY + Radius;
        if (p.y <= floor) return;
        velocityY = Mathf.Max(velocityY + GameConfig.Gravity * dt, GameConfig.MaxFallSpeed);
        p.y = Mathf.Max(floor, p.y + velocityY * dt);
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, -80f);
    }

    public void BounceOffGround()
    {
        var p = transform.position;
        p.y = GameConfig.GroundY + Radius + 0.05f;
        transform.position = p;
        velocityY = GameConfig.FlapVelocity;
    }

    public void BounceOffCeiling()
    {
        var p = transform.position;
        p.y = GameConfig.CeilingY - Radius - 0.05f;
        transform.position = p;
        velocityY = -2f;
    }

    public void StartInvulnerability()
    {
        invulnerableTimer = GameConfig.InvulnerableTime;
    }

    public void ApplyShrink()
    {
        shrinkTimer = GameConfig.ShrinkDuration;
    }

    void AnimateWing(float speed)
    {
        float k = Mathf.Sin(Time.time * speed);
        wing.localScale = new Vector3(0.38f, 0.22f * (0.4f + 0.6f * Mathf.Abs(k)), 1f);
        wing.localPosition = new Vector3(-0.14f, -0.02f + k * 0.06f, 0f);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }
}
