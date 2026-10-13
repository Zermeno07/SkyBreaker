using UnityEngine;

public enum PowerUpType
{
    ExtraLife, // Corazón: +1 vida (máximo 3)
    Shrink     // Hongo: Pico se encoge a la mitad durante 8 segundos
}

// Orbe flotante con un poder. Se activa al tocarlo.
public class PowerUp : MonoBehaviour
{
    Transform heart, mushroom;
    SpriteRenderer glow;
    float baseY;
    float phase;

    public PowerUpType Type { get; private set; }
    public Vector2 Position => transform.position;

    public static PowerUp Create(Transform parent)
    {
        var go = new GameObject("Poder");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PowerUp>();
    }

    void Awake()
    {
        glow = SpriteFactory.Shape("Orbe", transform, true, Color.white, Vector2.zero, new Vector2(0.85f, 0.85f), 3);

        heart = new GameObject("Corazon").transform;
        heart.SetParent(transform, false);
        var red = new Color(0.95f, 0.15f, 0.25f);
        SpriteFactory.Shape("L", heart, true, red, new Vector2(-0.11f, 0.06f), new Vector2(0.26f, 0.26f), 4);
        SpriteFactory.Shape("R", heart, true, red, new Vector2(0.11f, 0.06f), new Vector2(0.26f, 0.26f), 4);
        var tip = SpriteFactory.Shape("Punta", heart, false, red, new Vector2(0f, -0.06f), new Vector2(0.27f, 0.27f), 4);
        tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        mushroom = new GameObject("Hongo").transform;
        mushroom.SetParent(transform, false);
        SpriteFactory.Shape("Tallo", mushroom, false, new Color(1f, 0.95f, 0.85f), new Vector2(0f, -0.12f), new Vector2(0.18f, 0.24f), 4);
        SpriteFactory.Shape("Sombrero", mushroom, true, new Color(0.65f, 0.25f, 0.85f), new Vector2(0f, 0.06f), new Vector2(0.5f, 0.36f), 5);
        SpriteFactory.Shape("Punto1", mushroom, true, Color.white, new Vector2(-0.1f, 0.1f), new Vector2(0.09f, 0.09f), 6);
        SpriteFactory.Shape("Punto2", mushroom, true, Color.white, new Vector2(0.11f, 0.05f), new Vector2(0.08f, 0.08f), 6);
    }

    public void Spawn(PowerUpType type, Vector2 pos)
    {
        Type = type;
        transform.position = pos;
        baseY = pos.y;
        phase = Random.value * 6f;
        heart.gameObject.SetActive(type == PowerUpType.ExtraLife);
        mushroom.gameObject.SetActive(type == PowerUpType.Shrink);
        glow.color = type == PowerUpType.ExtraLife
            ? new Color(1f, 0.6f, 0.7f, 0.45f)
            : new Color(0.8f, 0.6f, 1f, 0.45f);
        gameObject.SetActive(true);
    }

    public void Tick(float dx)
    {
        var p = transform.position;
        p.x -= dx;
        p.y = baseY + Mathf.Sin(Time.time * 4f + phase) * 0.15f;
        transform.position = p;
        float s = 0.85f + Mathf.Sin(Time.time * 6f + phase) * 0.08f;
        glow.transform.localScale = new Vector3(s, s, 1f);
    }
}
