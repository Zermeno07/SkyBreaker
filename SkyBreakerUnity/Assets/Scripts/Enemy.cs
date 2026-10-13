using UnityEngine;

// Murciélago mecánico: entra por la derecha y vuela en línea recta más rápido que el escenario.
public class Enemy : MonoBehaviour
{
    Transform wingTop, wingBottom;

    public Vector2 Position => transform.position;

    public static Enemy Create(Transform parent)
    {
        var go = new GameObject("MurcielagoMecanico");
        go.transform.SetParent(parent, false);
        return go.AddComponent<Enemy>();
    }

    void Awake()
    {
        var metal = new Color(0.45f, 0.47f, 0.52f);
        var dark = new Color(0.3f, 0.3f, 0.35f);
        var red = new Color(0.9f, 0.15f, 0.15f);
        wingTop = SpriteFactory.Shape("AlaArriba", transform, false, red, new Vector2(0.1f, 0.25f), new Vector2(0.45f, 0.18f), 4).transform;
        wingBottom = SpriteFactory.Shape("AlaAbajo", transform, false, red, new Vector2(0.1f, -0.25f), new Vector2(0.45f, 0.18f), 4).transform;
        SpriteFactory.Shape("Cuerpo", transform, true, metal, Vector2.zero, new Vector2(0.62f, 0.55f), 5);
        SpriteFactory.Shape("Antena", transform, false, dark, new Vector2(0.05f, 0.33f), new Vector2(0.06f, 0.2f), 4);
        SpriteFactory.Shape("Ojo", transform, true, red, new Vector2(-0.14f, 0.06f), new Vector2(0.18f, 0.18f), 6);
        SpriteFactory.Shape("Boca", transform, false, dark, new Vector2(-0.1f, -0.13f), new Vector2(0.22f, 0.05f), 6);
    }

    public void Spawn(Vector2 pos)
    {
        transform.position = pos;
        gameObject.SetActive(true);
    }

    public void Tick(float dx)
    {
        transform.position += new Vector3(-dx, 0f, 0f);
        float k = Mathf.Sin(Time.time * 30f);
        wingTop.localPosition = new Vector3(0.1f, 0.25f + k * 0.08f, 0f);
        wingBottom.localPosition = new Vector3(0.1f, -0.25f - k * 0.08f, 0f);
    }
}
