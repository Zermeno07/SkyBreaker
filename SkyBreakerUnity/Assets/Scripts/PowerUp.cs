using UnityEngine;

public enum PowerUpType
{
    ExtraLife, // Corazón: +1 vida (máximo 3)
    Gun,       // Cañón: 5 disparos
    Shrink,    // Hongo: Pico se encoge a la mitad durante 8 segundos
    Ghost,     // Fantasma: atraviesa tuberías durante 5 segundos
    Shield,    // Escudo: absorbe el siguiente golpe
    Special    // Súper poder del personaje (cigarro, cerveza, papitas o moto)
}

// Orbe flotante con un poder. Se activa al tocarlo.
public class PowerUp : MonoBehaviour
{
    SpriteRenderer icon, glow, ring;
    float baseY;
    float phase;

    // Súper poder del personaje elegido; define el ícono del poder especial
    public static SuperPower CurrentSuper = SuperPower.None;

    public PowerUpType Type { get; private set; }
    public Vector2 Position => transform.position;

    public static string DisplayName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraLife: return "¡Vida extra!";
            case PowerUpType.Gun: return "¡Cañón! +" + GameConfig.GunShots + " disparos";
            case PowerUpType.Shrink: return "¡Encogido!";
            case PowerUpType.Ghost: return "¡Modo fantasma!";
            case PowerUpType.Special: return Characters.SuperName(CurrentSuper);
            default: return "¡Escudo!";
        }
    }

    public static Color TypeColor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraLife: return new Color(1f, 0.45f, 0.55f);
            case PowerUpType.Gun: return new Color(1f, 0.8f, 0.3f);
            case PowerUpType.Shrink: return new Color(0.8f, 0.5f, 1f);
            case PowerUpType.Ghost: return new Color(0.6f, 0.95f, 1f);
            case PowerUpType.Special: return new Color(1f, 0.6f, 0.15f);
            default: return new Color(0.4f, 0.75f, 1f);
        }
    }

    public static Sprite Icon(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraLife: return PixelArt.Get("Heart", PixelArt.Heart, 16f);
            case PowerUpType.Gun: return PixelArt.Get("Cannon", PixelArt.Cannon, 16f);
            case PowerUpType.Shrink: return PixelArt.Get("Mushroom", PixelArt.Mushroom, 16f);
            case PowerUpType.Ghost: return PixelArt.Get("Ghost", PixelArt.Ghost, 16f);
            case PowerUpType.Special: return Characters.SuperIcon(CurrentSuper);
            default: return PixelArt.Get("Shield", PixelArt.Shield, 16f);
        }
    }

    public static PowerUp Create(Transform parent)
    {
        var go = new GameObject("Poder");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PowerUp>();
    }

    void Awake()
    {
        glow = SpriteFactory.Shape("Brillo", transform, SpriteFactory.SoftCircle, Color.white, Vector2.zero, Vector2.one * 1.6f, 3);
        ring = SpriteFactory.Shape("Orbe", transform, SpriteFactory.Circle, Color.white, Vector2.zero, Vector2.one * 0.85f, 3);
        icon = SpriteFactory.Shape("Icono", transform, SpriteFactory.Square, Color.white, Vector2.zero, Vector2.one, 4);
    }

    public void Spawn(PowerUpType type, Vector2 pos)
    {
        Type = type;
        transform.position = pos;
        baseY = pos.y;
        phase = Random.value * 6f;
        icon.sprite = Icon(type);
        var c = TypeColor(type);
        glow.color = new Color(c.r, c.g, c.b, 0.55f);
        ring.color = new Color(1f, 1f, 1f, 0.35f);
        gameObject.SetActive(true);
    }

    public void Tick(float dx)
    {
        var p = transform.position;
        p.x -= dx;
        p.y = baseY + Mathf.Sin(Time.time * 4f + phase) * 0.15f;
        transform.position = p;
        float s = (Type == PowerUpType.Special ? 2.1f : 1.6f) + Mathf.Sin(Time.time * 6f + phase) * 0.2f;
        glow.transform.localScale = new Vector3(s, s, 1f);
        icon.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + phase) * 10f);
    }
}
