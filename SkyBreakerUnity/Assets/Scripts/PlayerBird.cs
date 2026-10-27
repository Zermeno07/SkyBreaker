using UnityEngine;

public enum TempPower { None, Shrink, Ghost }

public enum CharacterId { Pico = 0, Wicho = 1, Jorge = 2, Justo = 3, Paleteado = 4 }

// Súper poder exclusivo de cada personaje cabeza. Dura 10 tuberías.
public enum SuperPower
{
    None,
    Cigar, // Wicho: velocidad a tope, alucinación y humo
    Beer,  // Jorge: se sube a un carro y rompe las tuberías
    Chips, // Justo: las tuberías desaparecen y aparece una laguna
    Moto   // Paleteado: las tuberías se vuelven árboles y los destroza en moto
}

// Personajes seleccionables. Pico es pixel art; los demás son cabezas (fotos recortadas).
// Para agregar otro: crear su script de imagen (como JorgeArt.cs) y sumarlo aquí.
public static class Characters
{
    public const int Count = 5;

    public static string Name(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Wicho: return "Wicho L Pop's";
            case CharacterId.Jorge: return "Jorge Nitales";
            case CharacterId.Justo: return "Justo Prime";
            case CharacterId.Paleteado: return "Paleteado";
            default: return "Pico";
        }
    }

    public static bool IsHead(CharacterId id) => id != CharacterId.Pico;

    public static Sprite Head(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Wicho: return WichoArt.Head();
            case CharacterId.Jorge: return JorgeArt.Head();
            case CharacterId.Justo: return JustoArt.Head();
            case CharacterId.Paleteado: return PaleteadoArt.Head();
            default: return null;
        }
    }

    public static Sprite Preview(CharacterId id) =>
        IsHead(id) ? Head(id) : PixelArt.Get("PicoMid", PixelArt.PicoMid, 18f);

    // Colores de las partículas al recibir daño (plumas para Pico, estrellas para las cabezas)
    public static Color HurtColorA(CharacterId id) => IsHead(id) ? new Color(1f, 0.85f, 0.3f) : ZoneTheme.Hex(0x3b8bd8);
    public static Color HurtColorB(CharacterId id) => IsHead(id) ? new Color(1f, 1f, 1f) : ZoneTheme.Hex(0x7ccaff);

    // Posición aproximada de la boca dentro de la imagen (para el cigarro y las migajas)
    public static Vector2 Mouth(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Wicho: return new Vector2(0.17f, -0.32f);
            case CharacterId.Jorge: return new Vector2(0f, -0.35f);
            case CharacterId.Justo: return new Vector2(0.07f, -0.39f);
            case CharacterId.Paleteado: return new Vector2(0.17f, -0.25f);
            default: return new Vector2(0.45f, -0.1f);
        }
    }

    public static SuperPower Super(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Wicho: return SuperPower.Cigar;
            case CharacterId.Jorge: return SuperPower.Beer;
            case CharacterId.Justo: return SuperPower.Chips;
            case CharacterId.Paleteado: return SuperPower.Moto;
            default: return SuperPower.None;
        }
    }

    public static string SuperName(SuperPower sp)
    {
        switch (sp)
        {
            case SuperPower.Cigar: return "¡Cigarro! Velocidad a tope";
            case SuperPower.Beer: return "¡Cerveza! A chocar en carro";
            case SuperPower.Chips: return "¡Papitas! Aparece la laguna";
            case SuperPower.Moto: return "¡Moto! Los árboles no se salvan";
            default: return "";
        }
    }

    public static string SuperShortName(SuperPower sp)
    {
        switch (sp)
        {
            case SuperPower.Cigar: return "CIGARRO";
            case SuperPower.Beer: return "CERVEZA";
            case SuperPower.Chips: return "PAPITAS";
            case SuperPower.Moto: return "MOTO";
            default: return "";
        }
    }

    public static Sprite SuperIcon(SuperPower sp)
    {
        switch (sp)
        {
            case SuperPower.Cigar: return PixelArt.Get("Cigar", PixelArt.Cigar, 18f);
            case SuperPower.Beer: return PixelArt.Get("Beer", PixelArt.Beer, 16f);
            case SuperPower.Chips: return PixelArt.Get("Chips", PixelArt.Chips, 16f);
            case SuperPower.Moto: return PixelArt.Get("Moto", PixelArt.Moto, 24f);
            default: return null;
        }
    }

    // Multiplicador de velocidad del escenario mientras dura el súper poder
    public static float SuperSpeed(SuperPower sp)
    {
        switch (sp)
        {
            case SuperPower.Cigar: return 2.3f;
            case SuperPower.Beer: return 1.7f;
            case SuperPower.Moto: return 1.9f;
            case SuperPower.Chips: return 1.4f;
            default: return 1f;
        }
    }
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
    SpriteRenderer body, gun, shield, shieldRing, glow, prop, vehicle;
    float engineShake;
    Sprite up, mid, down;

    public CharacterId Character { get; private set; }
    public SuperPower Super { get; private set; }
    public bool HasVehicle => Super == SuperPower.Beer || Super == SuperPower.Moto;
    public TempPower Temp { get; private set; }
    public bool HasShield { get; private set; }
    public int Ammo { get; private set; }

    public bool IsInvulnerable => invulnerableTimer > 0f || Super != SuperPower.None;
    public bool IsShrunk => Temp == TempPower.Shrink;
    public bool IsGhost => Temp == TempPower.Ghost;
    public float TempRemaining01 => tempDuration > 0f ? tempTimer / tempDuration : 0f;
    public float Radius => GameConfig.BirdRadius * (Characters.IsHead(Character) ? 1.12f : 1f) * (IsShrunk ? 0.5f : 1f);
    public Vector2 Position => transform.position;
    public Vector2 GunTip => (Vector2)transform.position + (Vector2)(transform.rotation * new Vector3(0.55f * visual.localScale.x, -0.12f * visual.localScale.y, 0f));

    // Punto de donde sale el humo del cigarro / las migajas de las papitas
    public Vector2 MouthPoint
    {
        get
        {
            var m = Characters.Mouth(Character);
            var local = new Vector3((m.x + (Super == SuperPower.Cigar ? 0.62f : 0f)) * body.transform.localScale.x,
                m.y * body.transform.localScale.y, 0f) + body.transform.localPosition;
            return transform.TransformPoint(new Vector3(local.x * visual.localScale.x, local.y * visual.localScale.y, 0f));
        }
    }

    // Escape del carro o la moto
    public Vector2 ExhaustPoint => transform.TransformPoint(new Vector3(-1.1f, -0.25f, 0f));

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
        vehicle = SpriteFactory.Shape("Vehiculo", visual, SpriteFactory.Square, Color.white, Vector2.zero, Vector2.one, 11);
        prop = SpriteFactory.Shape("Cigarro", body.transform, PixelArt.Get("Cigar", PixelArt.Cigar, 24f), Color.white, Vector2.zero, Vector2.one, 11);
        vehicle.enabled = false;
        prop.enabled = false;
    }

    // Cambia la apariencia según el súper poder: cigarro en la boca, carro o moto.
    public void SetSuper(SuperPower sp)
    {
        Super = sp;
        prop.enabled = sp == SuperPower.Cigar;
        if (prop.enabled)
        {
            var m = Characters.Mouth(Character);
            prop.transform.localPosition = new Vector3(m.x + 0.3f, m.y, 0f);
            prop.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        }
        vehicle.enabled = HasVehicle;
        if (sp == SuperPower.Beer)
        {
            vehicle.sprite = PixelArt.Get("Car", PixelArt.Car, 12f);
            vehicle.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            body.transform.localPosition = new Vector3(-0.15f, 0.65f, 0f);
            body.transform.localScale = Vector3.one * 0.85f;
        }
        else if (sp == SuperPower.Moto)
        {
            vehicle.sprite = PixelArt.Get("Moto", PixelArt.Moto, 12f);
            vehicle.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            body.transform.localPosition = new Vector3(-0.05f, 0.8f, 0f);
            body.transform.localScale = Vector3.one * 0.85f;
        }
        else
        {
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one;
        }
        gun.enabled = Ammo > 0 && !HasVehicle;
    }

    public void SetCharacter(CharacterId id)
    {
        Character = id;
        bool head = Characters.IsHead(id);
        body.sprite = head ? Characters.Head(id) : mid;
        gun.transform.localPosition = head ? new Vector3(0.2f, -0.5f, 0f) : new Vector3(0.05f, -0.28f, 0f);
    }

    // Pico usa sus 3 cuadros de aleteo; las cabezas siempre muestran la misma imagen.
    void SetFrame(Sprite frame)
    {
        body.sprite = Characters.IsHead(Character) ? Characters.Head(Character) : frame;
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
        SetSuper(SuperPower.None);
        baseScale = 1f;
        squash = 0f;
        visual.localScale = Vector3.one;
        UpdateVisuals(0f);
    }

    public void Flap()
    {
        velocityY = HasVehicle ? 6f : GameConfig.FlapVelocity;
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
        // Con carro o moto se maneja sobre el suelo (aletear da un brinquito)
        float floor = GameConfig.GroundY + 0.75f;
        if (HasVehicle && p.y < floor)
        {
            p.y = floor;
            velocityY = 0f;
        }
        transform.position = p;
        float maxTilt = HasVehicle ? 12f : 30f;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(velocityY * (HasVehicle ? 1.5f : 4f), -maxTilt * 2f, maxTilt));
        if (HasVehicle)
        {
            engineShake += dt * 40f;
            vehicle.transform.localPosition = new Vector3(0f, (Super == SuperPower.Beer ? -0.15f : -0.2f) + Mathf.Sin(engineShake) * 0.025f, 0f);
        }

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
        bool blinkOff = invulnerableTimer > 0f && Super == SuperPower.None && ((int)(Time.time * 15f)) % 2 == 0;
        if (blinkOff) col.a *= 0.25f;
        body.color = col;

        gun.enabled = Ammo > 0 && !HasVehicle;
        if (prop.enabled) prop.color = new Color(1f, 1f, 1f, col.a);
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
