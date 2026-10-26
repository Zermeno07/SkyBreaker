using System.Collections.Generic;
using UnityEngine;

// Partículas sencillas reutilizables: explosiones, plumas, destellos, humo y escombros.
public class Fx
{
    class Particle
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float gravity, life, maxLife, spin, size0, size1;
        public Color color;
        public bool scrolls;
        public bool active;
    }

    readonly List<Particle> pool = new List<Particle>();
    readonly Transform root;

    public Fx(Transform parent)
    {
        root = new GameObject("Particulas").transform;
        root.SetParent(parent, false);
    }

    Particle Get()
    {
        foreach (var p in pool) if (!p.active) return p;
        var go = new GameObject("p");
        go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        var np = new Particle { t = go.transform, sr = sr };
        pool.Add(np);
        return np;
    }

    public void Burst(Vector2 pos, int count, Color color, float speed, float life, float size,
        float gravity = 0f, bool soft = false, bool scrolls = true, int order = 15, float endSize = 0f, Vector2? baseVel = null)
    {
        for (int i = 0; i < count; i++)
        {
            var p = Get();
            float ang = Random.value * Mathf.PI * 2f;
            float sp = speed * Random.Range(0.4f, 1f);
            p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp + (baseVel ?? Vector2.zero);
            p.gravity = gravity;
            p.maxLife = p.life = life * Random.Range(0.7f, 1.2f);
            p.spin = Random.Range(-360f, 360f);
            p.size0 = size * Random.Range(0.7f, 1.2f);
            p.size1 = endSize;
            p.color = color;
            p.scrolls = scrolls;
            p.active = true;
            p.sr.sprite = soft ? SpriteFactory.SoftCircle : SpriteFactory.Square;
            p.sr.sortingOrder = order;
            p.sr.color = color;
            p.t.position = pos;
            p.t.rotation = Quaternion.identity;
            p.t.localScale = Vector3.one * p.size0;
            p.t.gameObject.SetActive(true);
        }
    }

    public void Explosion(Vector2 pos, float scale = 1f)
    {
        Burst(pos, 6, new Color(1f, 0.95f, 0.6f, 0.9f), 1.5f * scale, 0.35f, 1.2f * scale, 0f, true, true, 16, 0.2f);
        Burst(pos, 14, new Color(1f, 0.55f, 0.15f), 5f * scale, 0.5f, 0.18f * scale, -4f);
        Burst(pos, 8, new Color(1f, 0.85f, 0.25f), 3.5f * scale, 0.4f, 0.12f * scale, -2f);
        Burst(pos, 6, new Color(0.35f, 0.35f, 0.4f, 0.7f), 1.2f * scale, 0.9f, 0.6f * scale, 1f, true, true, 14, 1.2f * scale);
    }

    public void Clear()
    {
        foreach (var p in pool) { p.active = false; p.t.gameObject.SetActive(false); }
    }

    public void Tick(float dt, float scrollDx)
    {
        foreach (var p in pool)
        {
            if (!p.active) continue;
            p.life -= dt;
            if (p.life <= 0f)
            {
                p.active = false;
                p.t.gameObject.SetActive(false);
                continue;
            }
            p.vel.y += p.gravity * dt;
            var pos = p.t.position;
            pos.x += p.vel.x * dt - (p.scrolls ? scrollDx : 0f);
            pos.y += p.vel.y * dt;
            p.t.position = pos;
            p.t.Rotate(0f, 0f, p.spin * dt);
            float k = 1f - p.life / p.maxLife;
            float s = Mathf.Lerp(p.size0, p.size1 > 0f ? p.size1 : p.size0 * 0.3f, k);
            p.t.localScale = new Vector3(s, s, 1f);
            var c = p.color;
            c.a *= 1f - k * k;
            p.sr.color = c;
        }
    }
}
