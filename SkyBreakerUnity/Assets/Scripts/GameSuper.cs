using UnityEngine;

// Súper poderes de los personajes cabeza. Cada uno dura 10 tuberías y durante ese tiempo
// el personaje no recibe daño:
//   Wicho L Pop's  - Cigarro: pasa a toda velocidad, el fondo se "derrite" y sale humo de su boca.
//   Jorge Nitales  - Cerveza: se sube a un carro y rompe las tuberías al chocar con ellas.
//   Justo Prime    - Papitas: las tuberías desaparecen y aparece una laguna.
//   Paleteado      - Moto: las tuberías se vuelven árboles y los destroza a su paso.
public partial class GameManager
{
    public const int SuperPipes = 10;

    SuperPower superActive;
    SuperPower superVisual;   // el último súper poder (para desvanecer sus efectos)
    int superPipesLeft;
    int superSpawnBudget;
    float superBlend;         // 0..1, sube al activar y baja al terminar
    int pipesUntilSpecial;
    float superTimerA, superTimerB;

    bool SuperOn => superActive != SuperPower.None;
    float SuperSpeedMultiplier => Mathf.Lerp(1f, Characters.SuperSpeed(superVisual), superBlend);

    void ResetSuper()
    {
        superActive = superVisual = SuperPower.None;
        superPipesLeft = 0;
        superSpawnBudget = 0;
        superBlend = 0f;
        pipesUntilSpecial = Random.Range(8, 12);
        bird.SetSuper(SuperPower.None);
        scenery.Trip = 0f;
        scenery.Lagoon = false;
        cam.orthographicSize = 5f;
        audioManager.SetMusicPitch(1f);
        foreach (var p in pipes) if (p.gameObject.activeSelf) p.SetTint(Color.white);
    }

    void ActivateSuper()
    {
        var sp = Characters.Super(character);
        if (sp == SuperPower.None) return;
        superActive = superVisual = sp;
        superPipesLeft = SuperPipes;

        // Las tuberías que ya vienen adelante también se transforman
        int converted = 0;
        foreach (var p in pipes)
        {
            if (!p.gameObject.activeSelf || p.Scored || p.X < bird.Position.x + 0.5f || converted >= SuperPipes) continue;
            MakeSpecialPipe(p);
            converted++;
        }
        superSpawnBudget = SuperPipes - converted;

        bird.SetSuper(sp);
        ShowBanner(Characters.SuperName(sp), "¡" + SuperPipes + " tuberías!", new Color(1f, 0.6f, 0.15f));
        Shake(0.4f);
        switch (sp)
        {
            case SuperPower.Cigar: PlaySfx(Sfx.Inhale); break;
            case SuperPower.Beer: PlaySfx(Sfx.Gulp); PlaySfx(Sfx.Rev, 0.8f, 0.8f); break;
            case SuperPower.Chips: PlaySfx(Sfx.Crunch); PlaySfx(Sfx.Splash); scenery.Lagoon = true; break;
            case SuperPower.Moto: PlaySfx(Sfx.Rev); break;
        }
    }

    void MakeSpecialPipe(PipePair p)
    {
        p.MakeSpecial(superActive);
        if (superActive == SuperPower.Chips)
        {
            // Desaparece con destellos
            var at = new Vector2(p.X, p.GapCenter);
            fx.Burst(at + new Vector2(0f, 2f), 10, new Color(1f, 0.9f, 0.4f), 3f, 0.6f, 0.12f);
            fx.Burst(at - new Vector2(0f, 2f), 10, new Color(1f, 0.9f, 0.4f), 3f, 0.6f, 0.12f);
        }
        else if (superActive == SuperPower.Moto)
        {
            fx.Burst(new Vector2(p.X, GameConfig.GroundY + 1.5f), 12, ZoneTheme.Hex(0x38b764), 3f, 0.6f, 0.15f);
        }
    }

    // Se llama al generar cada tubería nueva
    void OnPipeSpawned(PipePair p)
    {
        if (SuperOn && superSpawnBudget > 0)
        {
            superSpawnBudget--;
            MakeSpecialPipe(p);
        }
    }

    // Una tubería del súper poder quedó atrás (pasada, rota o desaparecida)
    void OnSpecialPipePassed()
    {
        if (!SuperOn) return;
        superPipesLeft--;
        if (superPipesLeft <= 0) EndSuper();
    }

    void EndSuper()
    {
        superActive = SuperPower.None;
        bird.SetSuper(SuperPower.None);
        bird.StartInvulnerability(1.5f);
        scenery.Lagoon = false;
        PlaySfx(Sfx.SuperEnd);
        AddPopup(bird.Position + new Vector2(0f, 0.8f), "¡Se acabó el efecto!", new Color(1f, 0.8f, 0.4f));
        pipesUntilSpecial = Random.Range(16, 25);
    }

    // ¿Toca generar el poder especial en esta tubería?
    bool ShouldSpawnSpecial()
    {
        if (Characters.Super(character) == SuperPower.None || SuperOn || Score < 4) return false;
        pipesUntilSpecial--;
        if (pipesUntilSpecial > 0) return false;
        pipesUntilSpecial = Random.Range(16, 25);
        return true;
    }

    void TickSuper(float dt)
    {
        superBlend = Mathf.MoveTowards(superBlend, SuperOn ? 1f : 0f, dt * 1.2f);
        if (superBlend <= 0f && !SuperOn)
        {
            if (superVisual != SuperPower.None) ResetSuperVisuals();
            return;
        }

        float trip = superVisual == SuperPower.Cigar ? superBlend : 0f;
        scenery.Trip = trip;
        cam.orthographicSize = 5f + Mathf.Sin(Time.time * 1.3f) * 0.35f * trip;
        audioManager.SetMusicPitch(1f + Mathf.Sin(Time.time * 2f) * 0.07f * trip + 0.1f * trip);
        if (trip > 0f)
        {
            foreach (var p in pipes)
            {
                if (!p.gameObject.activeSelf) continue;
                var hue = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f + p.X * 0.05f, 1f), 0.55f, 1f);
                p.SetTint(Color.Lerp(Color.white, hue, trip));
            }
        }

        if (!SuperOn) return;
        superTimerA -= dt;
        superTimerB -= dt;

        switch (superActive)
        {
            case SuperPower.Cigar:
                // Humo saliendo de la boca
                if (superTimerA <= 0f)
                {
                    superTimerA = 0.03f;
                    fx.Burst(bird.MouthPoint, 1, new Color(0.85f, 0.85f, 0.9f, 0.55f), 0.4f, 1.2f, 0.3f, 0.6f, true, true, 13, 1.4f, new Vector2(-0.6f, 0.5f));
                }
                if (superTimerB <= 0f)
                {
                    superTimerB = 0.5f;
                    fx.Burst(bird.MouthPoint, 2, new Color(1f, 0.5f, 0.2f), 1f, 0.3f, 0.06f);
                }
                break;

            case SuperPower.Chips:
                if (superTimerA <= 0f)
                {
                    superTimerA = 0.15f;
                    fx.Burst(bird.MouthPoint, 2, new Color(1f, 0.85f, 0.3f), 1.5f, 0.6f, 0.08f, -8f);
                }
                if (superTimerB <= 0f)
                {
                    superTimerB = 0.4f;
                    PlaySfx(Sfx.Crunch, 0.5f, Random.Range(0.9f, 1.2f));
                }
                break;

            case SuperPower.Beer:
            case SuperPower.Moto:
                if (superTimerA <= 0f)
                {
                    superTimerA = 0.06f;
                    fx.Burst(bird.ExhaustPoint, 1, new Color(0.5f, 0.5f, 0.55f, 0.6f), 0.5f, 0.6f, 0.25f, 0.5f, true, true, 9, 0.8f, new Vector2(-1f, 0.3f));
                }
                if (superTimerB <= 0f)
                {
                    superTimerB = 0.3f;
                    PlaySfx(Sfx.Engine, 0.6f, superActive == SuperPower.Moto ? 1.4f : 1f);
                }
                // Choca contra las tuberías (o árboles) y los destroza
                foreach (var p in pipes)
                {
                    if (!p.gameObject.activeSelf || !p.IsSpecial || p.Broken) continue;
                    float ahead = p.X - bird.Position.x;
                    if (ahead > 1.4f || ahead < -1f) continue;
                    SmashPipe(p);
                }
                break;
        }
    }

    void SmashPipe(PipePair p)
    {
        bool tree = p.IsTree;
        p.BreakAll();
        var at = new Vector2(p.X, GameConfig.GroundY + 1.2f);
        if (tree)
        {
            fx.Burst(at + new Vector2(0f, 1f), 22, ZoneTheme.Hex(0x38b764), 6f, 0.9f, 0.2f, -10f);
            fx.Burst(at + new Vector2(0f, 1f), 10, ZoneTheme.Hex(0x257179), 5f, 0.9f, 0.18f, -10f);
            fx.Burst(at, 10, ZoneTheme.Hex(0xa76d3f), 5f, 0.9f, 0.2f, -12f);
            PlaySfx(Sfx.Smash);
            AddPopup(at + new Vector2(0f, 2f), "¡ZAS!", new Color(0.6f, 1f, 0.4f));
        }
        else
        {
            var col = Theme.pipeMid;
            fx.Burst(at, 20, col, 7f, 0.9f, 0.22f, -12f);
            fx.Burst(new Vector2(p.X, p.GapCenter + p.GapSize * 0.5f + 1f), 14, col, 6f, 0.9f, 0.2f, -12f);
            fx.Explosion(at, 0.7f);
            PlaySfx(Sfx.Crash);
            AddPopup(at + new Vector2(0f, 2f), "¡CRASH!", new Color(1f, 0.5f, 0.3f));
        }
        Shake(0.45f);
        Vibrate();
        AddScore(1, null);
    }

    void ResetSuperVisuals()
    {
        superVisual = SuperPower.None;
        scenery.Trip = 0f;
        cam.orthographicSize = 5f;
        audioManager.SetMusicPitch(1f);
        foreach (var p in pipes) if (p.gameObject.activeSelf) p.SetTint(Color.white);
    }
}
