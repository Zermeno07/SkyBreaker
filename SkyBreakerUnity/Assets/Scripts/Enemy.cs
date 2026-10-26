using UnityEngine;

public enum EnemyType
{
    Bat,   // Murciélago mecánico: vuela en línea recta, más rápido que el escenario. Sin ataque.
    Wasp,  // Avispa robot: vuela en ondas y lanza aguijones apuntando a Pico.
    Drone, // Dron vigilante: se queda quieto entre tuberías y dispara láseres horizontales. Resiste 2 disparos.
    Crow   // Cuervo bombardero: cruza por arriba y suelta una bomba cuando Pico pasa por debajo.
}

// Comportamiento de cada enemigo. Todos avisan antes de atacar (parpadean y suenan)
// para que el jugador tenga tiempo de reaccionar.
public class Enemy : MonoBehaviour
{
    SpriteRenderer sr;
    SpriteRenderer alert;
    Sprite frameA, frameB;
    float t;
    float baseY;
    float attackTimer;
    float telegraph;
    float hitFlash;
    bool attacked;
    float attackSlow;

    public EnemyType Type { get; private set; }
    public int Hp { get; private set; }
    public Vector2 Position => transform.position;

    public float Radius
    {
        get
        {
            switch (Type)
            {
                case EnemyType.Wasp: return 0.36f;
                case EnemyType.Drone: return 0.42f;
                case EnemyType.Crow: return 0.42f;
                default: return 0.34f;
            }
        }
    }

    public int Points => Type == EnemyType.Drone || Type == EnemyType.Crow ? 3 : 2;

    public static string DisplayName(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Wasp: return "Avispa robot";
            case EnemyType.Drone: return "Dron vigilante";
            case EnemyType.Crow: return "Cuervo bombardero";
            default: return "Murciélago mecánico";
        }
    }

    public static string Hint(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Wasp: return "Vuela en ondas y lanza aguijones";
            case EnemyType.Drone: return "Dispara láseres: ¡cuidado cuando su ojo brille!";
            case EnemyType.Crow: return "Suelta bombas desde arriba";
            default: return "Vuela recto: esquívalo o dispárale";
        }
    }

    public static Enemy Create(Transform parent)
    {
        var go = new GameObject("Enemigo");
        go.transform.SetParent(parent, false);
        return go.AddComponent<Enemy>();
    }

    void Awake()
    {
        alert = SpriteFactory.Shape("Aviso", transform, SpriteFactory.SoftCircle, new Color(1f, 0.2f, 0.2f, 0f), Vector2.zero, Vector2.one * 1.8f, 4);
        sr = SpriteFactory.Shape("Sprite", transform, SpriteFactory.Square, Color.white, Vector2.zero, Vector2.one, 5);
    }

    // attackSlow: multiplicador del tiempo entre ataques (más alto = más fácil)
    public void Spawn(EnemyType type, Vector2 pos, float attackSlow)
    {
        Type = type;
        this.attackSlow = attackSlow;
        transform.position = pos;
        baseY = pos.y;
        t = Random.value * 3f;
        telegraph = 0f;
        hitFlash = 0f;
        attacked = false;
        switch (type)
        {
            case EnemyType.Bat:
                frameA = PixelArt.Get("BatA", PixelArt.BatA, 17f);
                frameB = PixelArt.Get("BatB", PixelArt.BatB, 17f);
                Hp = 1;
                break;
            case EnemyType.Wasp:
                frameA = PixelArt.Get("WaspA", PixelArt.WaspA, 17f);
                frameB = PixelArt.Get("WaspB", PixelArt.WaspB, 17f);
                Hp = 1;
                attackTimer = 1.6f * attackSlow;
                break;
            case EnemyType.Drone:
                frameA = PixelArt.Get("DroneA", PixelArt.DroneA, 16f);
                frameB = PixelArt.Get("DroneB", PixelArt.DroneB, 16f);
                Hp = 2;
                attackTimer = 1.2f * attackSlow;
                break;
            case EnemyType.Crow:
                frameA = PixelArt.Get("CrowA", PixelArt.CrowA, 17f);
                frameB = PixelArt.Get("CrowB", PixelArt.CrowB, 17f);
                Hp = 1;
                break;
        }
        sr.sprite = frameA;
        gameObject.SetActive(true);
    }

    public void Tick(float dt, float dx, Vector2 bird, float halfWidth, GameManager gm)
    {
        t += dt;
        var p = transform.position;
        bool onScreen = p.x < halfWidth - 0.3f;
        float animSpeed = 8f;

        switch (Type)
        {
            case EnemyType.Bat:
                p.x -= dx + 1.6f * dt;
                p.y = baseY + Mathf.Sin(t * 5f) * 0.12f;
                animSpeed = 10f;
                break;

            case EnemyType.Wasp:
                p.x -= dx + 1f * dt;
                p.y = baseY + Mathf.Sin(t * 2.2f) * 1.1f;
                animSpeed = 20f;
                if (onScreen && p.x > bird.x + 2f) UpdateAttack(dt, 0.5f, 2.8f, gm, () =>
                {
                    Vector2 dir = (bird - (Vector2)transform.position).normalized;
                    gm.SpawnEnemyProjectile(ProjectileKind.Stinger, (Vector2)transform.position + dir * 0.4f, dir * 4.5f);
                    gm.PlaySfx(Sfx.Stinger);
                });
                else telegraph = 0f;
                break;

            case EnemyType.Drone:
                p.x -= dx;
                p.y = baseY + Mathf.Sin(t * 1.5f) * 0.25f;
                animSpeed = 16f;
                if (onScreen && p.x > bird.x + 1.5f) UpdateAttack(dt, 0.7f, 2.6f, gm, () =>
                {
                    gm.SpawnEnemyProjectile(ProjectileKind.Laser, (Vector2)transform.position + new Vector2(-0.5f, 0f), new Vector2(-5.5f, 0f));
                    gm.PlaySfx(Sfx.Laser);
                });
                else telegraph = 0f;
                break;

            case EnemyType.Crow:
                p.x -= dx + 1.2f * dt;
                p.y = baseY + Mathf.Sin(t * 3f) * 0.15f;
                animSpeed = 7f;
                // Suelta la bomba calculando cuánto tarda en caer hasta la altura de Pico
                float ahead = p.x - bird.x;
                float screenSpeed = (dt > 0f ? dx / dt : 0f) + 1.2f;
                float fallTime = Mathf.Sqrt(2f * Mathf.Max(0.3f, p.y - bird.y) / 7f);
                if (!attacked && onScreen && ahead > 0.5f && ahead <= fallTime * screenSpeed)
                {
                    attacked = true;
                    gm.SpawnEnemyProjectile(ProjectileKind.Bomb, (Vector2)transform.position + new Vector2(0f, -0.4f), new Vector2(-1.2f, 0f));
                    gm.PlaySfx(Sfx.BombWhistle);
                }
                break;
        }

        transform.position = p;
        sr.sprite = ((int)(t * animSpeed)) % 2 == 0 ? frameA : frameB;

        // Destello blanco al recibir daño y brillo rojo de aviso antes de atacar
        hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 5f);
        sr.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0.3f), hitFlash);
        float blink = telegraph > 0f ? (0.35f + 0.35f * Mathf.Sin(Time.time * 30f)) : 0f;
        alert.color = new Color(1f, 0.25f, 0.2f, blink);
    }

    void UpdateAttack(float dt, float warnTime, float interval, GameManager gm, System.Action fire)
    {
        attackTimer -= dt;
        if (attackTimer <= warnTime && telegraph <= 0f)
        {
            telegraph = warnTime;
            gm.PlaySfx(Type == EnemyType.Drone ? Sfx.Charge : Sfx.EnemyWarn, 0.6f);
        }
        if (telegraph > 0f) telegraph -= dt;
        if (attackTimer <= 0f)
        {
            fire();
            telegraph = 0f;
            attackTimer = interval * attackSlow * Random.Range(0.85f, 1.15f);
        }
    }

    // Devuelve true si el enemigo fue destruido.
    public bool TakeDamage()
    {
        Hp--;
        hitFlash = 1f;
        return Hp <= 0;
    }
}
