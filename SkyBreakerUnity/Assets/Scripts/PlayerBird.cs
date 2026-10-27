using UnityEngine;

public enum TempPower { None, Shrink, Ghost }

public enum CharacterId { Pico = 0, Wicho = 1 }

public static class Characters
{
    public const int Count = 2;

    public static string Name(CharacterId id) => id == CharacterId.Wicho ? "Wicho L Pop's" : "Pico";

    public static Sprite Preview(CharacterId id) =>
        id == CharacterId.Wicho ? WichoArt.Head() : PixelArt.Get("PicoMid", PixelArt.PicoMid, 18f);

    // Colores de las partículas al recibir daño (plumas para Pico, estrellas para Wicho)
    public static Color HurtColorA(CharacterId id) => id == CharacterId.Wicho ? new Color(1f, 0.85f, 0.3f) : ZoneTheme.Hex(0x3b8bd8);
    public static Color HurtColorB(CharacterId id) => id == CharacterId.Wicho ? new Color(1f, 1f, 1f) : ZoneTheme.Hex(0x7ccaff);
}

// Pico: aleteo, gravedad, animación de pixel art, invulnerabilidad tras un golpe y poderes.
// Solo puede haber un poder temporal (encoger o fantasma) a la vez; el escudo y el arma se suman aparte.
public class PlayerBird : MonoBehaviour
{
    float velocityY;
    float invulnerableTimer;
    float tempTimer;
    float tempDuration;
    float flapAnim;
    float squash;
    float hurtFlash;
    float baseScale = 1f;

    Transform visual;
    SpriteRenderer body, gun, shield, shieldRing, glow;
    Sprite up, mid, down;

    public CharacterId Character { get; private set; }
    public TempPower Temp { get; private set; }
    public bool HasShield { get; private set; }
    public int Ammo { get; private set; }

    public bool IsInvulnerable => invulnerableTimer > 0f;
    public bool IsShrunk => Temp == TempPower.Shrink;
    public bool IsGhost => Temp == TempPower.Ghost;
    public float TempRemaining01 => tempDuration > 0f ? tempTimer / tempDuration : 0f;
    public float Radius => GameConfig.BirdRadius * (Character == CharacterId.Wicho ? 1.12f : 1f) * (IsShrunk ? 0.5f : 1f);
    public Vector2 Position => transform.position;
    public Vector2 GunTip => (Vector2)transform.position + (Vector2)(transform.rotation * new Vector3(0.55f * visual.localScale.x, -0.12f * visual.localScale.y, 0f));

    public static PlayerBird Create(Transform parent)
    {
        var go = new GameObject("Pico");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PlayerBird>();
    }

    void Awake()
    {
        up = PixelArt.Get("PicoUp", PixelArt.PicoUp, 18f);
        mid = PixelArt.Get("PicoMid", PixelArt.PicoMid, 18f);
        down = PixelArt.Get("PicoDown", PixelArt.PicoDown, 18f);

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        glow = SpriteFactory.Shape("Brillo", visual, SpriteFactory.SoftCircle, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one * 2f, 9);
        body = SpriteFactory.Shape("Cuerpo", visual, mid, Color.white, Vector2.zero, Vector2.one, 10);
        gun = SpriteFactory.Shape("Canon", visual, PixelArt.Get("Gun", PixelArt.Gun, 18f), Color.white, new Vector2(0.05f, -0.28f), Vector2.one, 11);
        shield = SpriteFactory.Shape("Escudo", visual, SpriteFactory.SoftCircle, new Color(0.4f, 0.8f, 1f, 0.35f), Vector2.zero, Vector2.one * 1.6f, 12);
        shieldRing = SpriteFactory.Shape("AroEscudo", visual, SpriteFactory.Circle, new Color(0.6f, 0.9f, 1f, 0.25f), Vector2.zero, Vector2.one * 1.25f, 8);
    }

    public void SetCharacter(CharacterId id)
    {
        Character = id;
        bool wicho = id == CharacterId.Wicho;
        body.sprite = wicho ? WichoArt.Head() : mid;
        gun.transform.localPosition = wicho ? new Vector3(0.2f, -0.5f, 0f) : new Vector3(0.05f, -0.28f, 0f);
    }

    // Pico usa sus 3 cuadros de aleteo; Wicho siempre muestra la misma imagen.
    void SetFrame(Sprite frame)
    {
        body.sprite = Character == CharacterId.Wicho ? WichoArt.Head() : frame;
    }

    public void ResetBird(float x)
    {
        transform.position = new Vector3(x, 0f, 0f);
        transform.rotation = Quaternion.identity;
        velocityY = 0f;
        invulnerableTimer = 0f;
        Temp = TempPower.None;
        tempTimer = 0f;
        HasShield = false;
        Ammo = 0;
        hurtFlash = 0f;
        baseScale = 1f;
        squash = 0f;
        visual.localScale = Vector3.one;
        UpdateVisuals(0f);
    }

    public void Flap()
    {
        velocityY = GameConfig.FlapVelocity;
        flapAnim = 0.25f;
        squash = 1f;
    }

    public void IdleBob(float x)
    {
        transform.position = new Vector3(x, Mathf.Sin(Time.time * 3f) * 0.3f, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + 1f) * 6f);
        int f = (int)(Time.time * 10f) % 4;
        SetFrame(f == 0 ? up : f == 2 ? down : mid);
        UpdateVisuals(Time.deltaTime);
    }

    public void TickPhysics(float dt)
    {
        velocityY = Mathf.Max(velocityY + GameConfig.Gravity * dt, GameConfig.MaxFallSpeed);
        var p = transform.position;
        p.y += velocityY * dt;
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(velocityY * 4f, -70f, 30f));

        if (invulnerableTimer > 0f) invulnerableTimer -= dt;
        if (Temp != TempPower.None)
        {
            tempTimer -= dt;
            if (tempTimer <= 0f) Temp = TempPower.None;
        }

        // Animación de alas: rápida al aletear, planeo al caer
        flapAnim -= dt;
        if (flapAnim > 0f)
        {
            float k = flapAnim / 0.25f;
            SetFrame(k > 0.66f ? down : k > 0.33f ? mid : up);
        }
        else
        {
            SetFrame(velocityY < -6f ? up : mid);
        }
        UpdateVisuals(dt);
    }

    // Caída tras perder la última vida
    public void TickDead(float dt)
    {
        var p = transform.position;
        float floor = GameConfig.GroundY + Radius;
        if (p.y > floor)
        {
            velocityY = Mathf.Max(velocityY + GameConfig.Gravity * dt, GameConfig.MaxFallSpeed);
            p.y = Mathf.Max(floor, p.y + velocityY * dt);
            transform.position = p;
            transform.Rotate(0f, 0f, -540f * dt);
        }
        SetFrame(mid);
        Temp = TempPower.None;
        HasShield = false;
        Ammo = 0;
        invulnerableTimer = 0f;
        UpdateVisuals(dt);
    }

    void UpdateVisuals(float dt)
    {
        float targetScale = IsShrunk ? 0.5f : 1f;
        baseScale = Mathf.MoveTowards(baseScale, targetScale, dt * 3f);
        squash = Mathf.MoveTowards(squash, 0f, dt * 6f);
        visual.localScale = new Vector3(baseScale * (1f - 0.12f * squash), baseScale * (1f + 0.15f * squash), 1f);

        hurtFlash = Mathf.MoveTowards(hurtFlash, 0f, dt * 3f);
        var col = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), hurtFlash);
        if (IsGhost) col.a = 0.45f + 0.15f * Mathf.Sin(Time.time * 12f);
        bool blinkOff = IsInvulnerable && ((int)(Time.time * 15f)) % 2 == 0;
        if (blinkOff) col.a *= 0.25f;
        body.color = col;

        gun.enabled = Ammo > 0;
        shield.enabled = HasShield;
        shieldRing.enabled = HasShield;
        if (HasShield)
        {
            float pulse = 1.6f + Mathf.Sin(Time.time * 5f) * 0.08f;
            shield.transform.localScale = new Vector3(pulse, pulse, 1f);
        }

        // Brillo de color según el poder activo
        Color g = new Color(1f, 1f, 1f, 0f);
        if (IsGhost) g = new Color(0.6f, 0.9f, 1f, 0.45f);
        else if (IsShrunk) g = new Color(0.8f, 0.5f, 1f, 0.45f);
        glow.color = Color.Lerp(glow.color, g, Mathf.Clamp01(dt * 8f));
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

    public void StartInvulnerability(float time)
    {
        invulnerableTimer = time;
        hurtFlash = 1f;
    }

    public void ApplyTemp(TempPower power, float duration)
    {
        Temp = power;
        tempTimer = tempDuration = duration;
    }

    public void GiveShield() { HasShield = true; }
    public void BreakShield() { HasShield = false; }

    public void AddAmmo(int n) { Ammo = Mathf.Min(GameConfig.MaxAmmo, Ammo + n); }

    public bool UseAmmo()
    {
        if (Ammo <= 0) return false;
        Ammo--;
        return true;
    }
}
