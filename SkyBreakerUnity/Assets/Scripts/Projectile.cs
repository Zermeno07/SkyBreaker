using UnityEngine;

public enum ProjectileKind
{
    PlayerShot, // disparo del arma de Pico
    Stinger,    // aguijón de la avispa robot (apunta a Pico)
    Laser,      // láser del dron vigilante (horizontal)
    Bomb        // bomba del cuervo bombardero (cae con gravedad)
}

// Proyectil reutilizable. Los del jugador destruyen enemigos y tuberías agrietadas;
// los de los enemigos le quitan una vida a Pico.
public class Projectile : MonoBehaviour
{
    SpriteRenderer sr;
    SpriteRenderer glow;
    Vector2 velocity;
    float gravity;

    public ProjectileKind Kind { get; private set; }
    public Vector2 Position => transform.position;
    public bool FromPlayer => Kind == ProjectileKind.PlayerShot;

    public float Radius
    {
        get
        {
            switch (Kind)
            {
                case ProjectileKind.Bomb: return 0.22f;
                case ProjectileKind.Laser: return 0.14f;
                case ProjectileKind.Stinger: return 0.12f;
                default: return 0.18f;
            }
        }
    }

    public static Projectile Create(Transform parent)
    {
        var go = new GameObject("Proyectil");
        go.transform.SetParent(parent, false);
        return go.AddComponent<Projectile>();
    }

    void Awake()
    {
        glow = SpriteFactory.Shape("Brillo", transform, SpriteFactory.SoftCircle, Color.white, Vector2.zero, Vector2.one * 0.8f, 6);
        sr = SpriteFactory.Shape("Sprite", transform, SpriteFactory.Square, Color.white, Vector2.zero, Vector2.one, 7);
    }

    public void Launch(ProjectileKind kind, Vector2 pos, Vector2 vel)
    {
        Kind = kind;
        transform.position = pos;
        velocity = vel;
        gravity = kind == ProjectileKind.Bomb ? -GameConfig.BombGravity : 0f;
        switch (kind)
        {
            case ProjectileKind.PlayerShot:
                sr.sprite = PixelArt.Get("Shot", PixelArt.Shot, 14f);
                glow.color = new Color(1f, 0.85f, 0.3f, 0.6f);
                break;
            case ProjectileKind.Stinger:
                sr.sprite = PixelArt.Get("Stinger", PixelArt.Stinger, 14f);
                glow.color = new Color(1f, 0.9f, 0.3f, 0.35f);
                break;
            case ProjectileKind.Laser:
                sr.sprite = PixelArt.Get("Laser", PixelArt.Laser, 14f);
                glow.color = new Color(1f, 0.2f, 0.2f, 0.6f);
                break;
            case ProjectileKind.Bomb:
                sr.sprite = PixelArt.Get("Bomb", PixelArt.Bomb, 16f);
                glow.color = new Color(1f, 0.5f, 0.2f, 0.25f);
                break;
        }
        Orient();
        gameObject.SetActive(true);
    }

    void Orient()
    {
        if (Kind == ProjectileKind.Bomb)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f) * 10f);
            return;
        }
        float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    // dx: desplazamiento del escenario (las bombas se mueven con el mundo)
    public void Tick(float dt, float dx)
    {
        velocity.y += gravity * dt;
        var p = transform.position;
        p.x += velocity.x * dt - (Kind == ProjectileKind.Bomb ? dx : 0f);
        p.y += velocity.y * dt;
        transform.position = p;
        Orient();
        float pulse = 0.8f + Mathf.Sin(Time.time * 20f) * 0.15f;
        glow.transform.localScale = new Vector3(pulse, pulse, 1f);
    }
}
