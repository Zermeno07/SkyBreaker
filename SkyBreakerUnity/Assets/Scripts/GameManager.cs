using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver }

// Administrador del juego: estados (menú, jugando, pausa, fin de partida), generación de
// tuberías, poderes y enemigos con pooling, colisiones, puntuación, récord y escenarios.
// La interfaz está en GameUI.cs (misma clase, archivo aparte).
public partial class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int Lives { get; private set; }
    public float Speed { get; private set; }

    Camera cam;
    Transform world;
    PlayerBird bird;
    Scenery scenery;
    Fx fx;
    AudioManager audioManager;

    readonly List<PipePair> pipes = new List<PipePair>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<PowerUp> powerUps = new List<PowerUp>();
    readonly List<Projectile> projectiles = new List<Projectile>();

    float distanceSinceLastPipe;
    float lastGapCenter;
    int pipesUntilPowerUp;
    int pipesSinceEnemy;
    int unlockedEnemies;
    EnemyType? forcedEnemy;
    bool gunGiftPending;
    bool newRecord;
    float gameOverTime;
    float shotCooldown;
    float shake;

    // Escenarios
    ZoneId startZone;
    CharacterId character;
    ZoneId currentZone;
    float zoneTransition;     // > 0 mientras hay un cambio de escenario en curso
    ZoneId pendingZone;
    bool zoneSwapped;

    // Estadísticas de la partida
    int enemiesDestroyed;
    int powerUpsTaken;
    int zonesVisited;

    float HalfWidth => cam.orthographicSize * cam.aspect;
    float BirdX => -Mathf.Min(HalfWidth * 0.45f, 4f);
    float Difficulty01 => Mathf.Clamp01(Score / (float)GameConfig.MasteryScore);
    ZoneTheme Theme => ZoneTheme.All[(int)currentZone];

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;

        SetupCamera();
        world = new GameObject("Mundo").transform;
        audioManager = AudioManager.Create(transform);
        scenery = new Scenery(world, cam);
        scenery.Thunder += () => PlaySfx(Sfx.Thunder, 0.8f);
        fx = new Fx(world);
        bird = PlayerBird.Create(world);
        HighScore = PlayerPrefs.GetInt(GameConfig.HighScoreKey, 0);
        startZone = (ZoneId)Mathf.Clamp(PlayerPrefs.GetInt(GameConfig.StartZoneKey, 0), 0, 2);
        vibration = PlayerPrefs.GetInt(GameConfig.VibrationKey, 1) == 1;
        character = (CharacterId)Mathf.Clamp(PlayerPrefs.GetInt(GameConfig.CharacterKey, 0), 0, Characters.Count - 1);
        bird.SetCharacter(character);
        PowerUp.CurrentSuper = Characters.Super(character);
        GoToMenu();
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
    }

    // ---------------------------------------------------------------- Estados

    void GoToMenu()
    {
        Time.timeScale = 1f;
        ClearWorld();
        Score = 0;
        currentZone = startZone;
        scenery.Build(startZone);
        bird.ResetBird(BirdX);
        ResetSuper();
        State = GameState.Menu;
        menuScreen = MenuScreen.Main;
        audioManager.SetMusicDucked(false);
        audioManager.SetIntensity(0f);
        audioManager.PlaySong(0);
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        ClearWorld();
        Score = 0;
        Lives = GameConfig.StartLives;
        Speed = GameConfig.StartSpeed;
        newRecord = false;
        distanceSinceLastPipe = GameConfig.PipeSpacing * 0.5f;
        lastGapCenter = 0f;
        pipesUntilPowerUp = GameConfig.FirstPowerUpPipes;
        pipesSinceEnemy = 0;
        unlockedEnemies = 0;
        forcedEnemy = null;
        gunGiftPending = false;
        enemiesDestroyed = powerUpsTaken = 0;
        zonesVisited = 1;
        zoneTransition = 0f;
        if (currentZone != startZone || scenery.Theme == null)
        {
            currentZone = startZone;
            scenery.Build(startZone);
        }
        bird.ResetBird(BirdX);
        ResetSuper();
        bird.Flap();
        State = GameState.Playing;
        audioManager.SetMusicDucked(false);
        audioManager.PlaySong(Theme.song);
        ShowBanner(Theme.name, "¡Supera tu récord!", Theme.uiAccent);
    }

    void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        pauseScreen = MenuScreen.Main;
        Time.timeScale = 0f;
        audioManager.SetMusicDucked(true);
    }

    void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        audioManager.SetMusicDucked(false);
    }

    void EndGame()
    {
        State = GameState.GameOver;
        gameOverTime = Time.unscaledTime;
        audioManager.SetMusicDucked(true);
        PlaySfx(Sfx.GameOver);
        if (Score > HighScore)
        {
            HighScore = Score;
            newRecord = true;
            PlayerPrefs.SetInt(GameConfig.HighScoreKey, HighScore);
            PlayerPrefs.Save();
            Invoke(nameof(PlayRecordJingle), 0.8f);
        }
    }

    void PlayRecordJingle() { PlaySfx(Sfx.Record); }

    void ClearWorld()
    {
        foreach (var p in pipes) p.gameObject.SetActive(false);
        foreach (var e in enemies) e.gameObject.SetActive(false);
        foreach (var p in powerUps) p.gameObject.SetActive(false);
        foreach (var p in projectiles) p.gameObject.SetActive(false);
        fx.Clear();
        popups.Clear();
        bannerTimer = 0f;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Pause();
    }

    // ---------------------------------------------------------------- Bucle principal

    void Update()
    {
        float dt = Time.deltaTime;
        switch (State)
        {
            case GameState.Menu:
                bird.IdleBob(BirdX);
                scenery.Tick(dt, GameConfig.StartSpeed * 0.5f * dt, false);
                fx.Tick(dt, 0f);
                break;
            case GameState.Playing:
                TickPlaying(dt);
                break;
            case GameState.GameOver:
                bird.TickDead(dt);
                scenery.Tick(dt, 0f, false);
                fx.Tick(dt, 0f);
                break;
        }
        TickPopups(Time.unscaledDeltaTime);

        // Temblor de cámara
        shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 2f);
        var off = State == GameState.Paused ? Vector2.zero : Random.insideUnitCircle * shake * 0.3f;
        cam.transform.position = new Vector3(off.x, off.y, -10f);
    }

    void TickPlaying(float dt)
    {
        Speed = Mathf.Lerp(GameConfig.StartSpeed, GameConfig.MaxSpeed, Difficulty01) * SuperSpeedMultiplier;
        float dx = Speed * dt;
        float halfW = HalfWidth;
        shotCooldown -= dt;

        bird.TickPhysics(dt);
        TickSuper(dt);
        scenery.Tick(dt, dx, true);
        fx.Tick(dt, dx);
        TickZoneTransition(dt);
        audioManager.SetIntensity(Score / 60f);

        // Plumitas cuando está en modo fantasma
        if (bird.IsGhost && Random.value < 0.4f)
            fx.Burst(bird.Position, 1, new Color(0.6f, 0.95f, 1f, 0.6f), 0.3f, 0.5f, 0.35f, 0f, true, true, 9);

        // Generación de tuberías por distancia recorrida
        distanceSinceLastPipe += dx;
        if (distanceSinceLastPipe >= GameConfig.PipeSpacing)
        {
            distanceSinceLastPipe -= GameConfig.PipeSpacing;
            SpawnPipe(halfW + GameConfig.PipeWidth);
        }

        Vector2 c = bird.Position;
        float r = bird.Radius;

        foreach (var pipe in pipes)
        {
            if (!pipe.gameObject.activeSelf) continue;
            pipe.Tick(dx);
            pipe.SetFaded(bird.IsGhost);
            if (!pipe.Scored && pipe.X + pipe.HalfWidth < c.x - r)
            {
                pipe.Scored = true;
                AddScore(1, null);
                PlaySfx(Sfx.Score, 0.7f);
                if (pipe.IsSpecial) OnSpecialPipePassed();
            }
            if (pipe.X < -halfW - GameConfig.PipeWidth * 2f) pipe.gameObject.SetActive(false);
        }

        foreach (var e in enemies)
        {
            if (!e.gameObject.activeSelf) continue;
            e.Tick(dt, dx, c, halfW, this);
            if (e.Position.x < -halfW - 2f) e.gameObject.SetActive(false);
        }

        foreach (var p in powerUps)
        {
            if (!p.gameObject.activeSelf) continue;
            p.Tick(dx);
            if (p.Position.x < -halfW - 2f) { p.gameObject.SetActive(false); continue; }
            if ((p.Position - c).sqrMagnitude < Sqr(r + GameConfig.PowerUpRadius))
                Collect(p);
        }

        TickProjectiles(dt, dx, halfW);
        if (State == GameState.Playing) CheckCollisions(c, r);
    }

    void TickProjectiles(float dt, float dx, float halfW)
    {
        foreach (var pr in projectiles)
        {
            if (!pr.gameObject.activeSelf) continue;
            pr.Tick(dt, dx);
            var pos = pr.Position;
            if (pos.x > halfW + 2f || pos.x < -halfW - 2f || pos.y < GameConfig.GroundY - 1f || pos.y > GameConfig.CeilingY + 2f)
            {
                pr.gameObject.SetActive(false);
                continue;
            }

            if (pr.FromPlayer)
            {
                PlayerShotHits(pr);
                continue;
            }

            // Las bombas explotan al tocar el suelo
            if (pr.Kind == ProjectileKind.Bomb && pos.y - pr.Radius <= GameConfig.GroundY)
            {
                pr.gameObject.SetActive(false);
                fx.Explosion(new Vector2(pos.x, GameConfig.GroundY + 0.2f), 0.8f);
                PlaySfx(Sfx.BombBoom, 0.7f);
                continue;
            }

            if (State == GameState.Playing && !bird.IsInvulnerable &&
                (pos - bird.Position).sqrMagnitude < Sqr(bird.Radius + pr.Radius * GameConfig.EnemyHitboxScale))
            {
                pr.gameObject.SetActive(false);
                if (pr.Kind == ProjectileKind.Bomb) fx.Explosion(pos, 0.8f);
                Hurt();
            }
        }
    }

    void PlayerShotHits(Projectile shot)
    {
        var pos = shot.Position;
        foreach (var e in enemies)
        {
            if (!e.gameObject.activeSelf) continue;
            if ((e.Position - pos).sqrMagnitude < Sqr(e.Radius + shot.Radius + GameConfig.ShotHitBonus))
            {
                shot.gameObject.SetActive(false);
                if (e.TakeDamage()) DestroyEnemy(e, true);
                else
                {
                    fx.Burst(pos, 6, new Color(1f, 0.9f, 0.5f), 3f, 0.25f, 0.1f);
                    PlaySfx(Sfx.Hit, 0.5f, 1.5f);
                }
                return;
            }
        }
        foreach (var pr in projectiles)
        {
            if (pr == shot || !pr.gameObject.activeSelf || pr.FromPlayer) continue;
            if ((pr.Position - pos).sqrMagnitude < Sqr(pr.Radius + shot.Radius + 0.1f))
            {
                shot.gameObject.SetActive(false);
                pr.gameObject.SetActive(false);
                fx.Explosion(pr.Position, 0.5f);
                PlaySfx(Sfx.Explosion, 0.5f, 1.3f);
                return;
            }
        }
        foreach (var pipe in pipes)
        {
            if (!pipe.gameObject.activeSelf) continue;
            int hit = pipe.HitByShot(pos, shot.Radius, out var at);
            if (hit == 0) continue;
            shot.gameObject.SetActive(false);
            if (hit == 2)
            {
                fx.Burst(pos, 18, ZoneTheme.Hex(0xa8552a), 5f, 0.7f, 0.2f, -12f);
                fx.Burst(pos, 8, ZoneTheme.Hex(0x6b2e14), 3f, 0.8f, 0.3f, -12f);
                fx.Burst(pos, 4, new Color(0.7f, 0.6f, 0.5f, 0.5f), 1f, 0.8f, 0.8f, 0f, true, true, 14, 1.5f);
                PlaySfx(Sfx.PipeBreak);
                AddScore(1, pos);
                Shake(0.3f);
            }
            else
            {
                fx.Burst(at, 5, new Color(1f, 0.9f, 0.6f), 2.5f, 0.2f, 0.08f);
                PlaySfx(Sfx.Hit, 0.25f, 2f);
            }
            return;
        }
    }

    void DestroyEnemy(Enemy e, bool byPlayer)
    {
        e.gameObject.SetActive(false);
        fx.Explosion(e.Position);
        PlaySfx(Sfx.Explosion);
        Shake(0.35f);
        if (byPlayer)
        {
            enemiesDestroyed++;
            AddScore(e.Points, e.Position);
        }
    }

    void CheckCollisions(Vector2 c, float r)
    {
        // El suelo y el techo siempre bloquean; quitan vida si no hay invulnerabilidad.
        if (c.y - r <= GameConfig.GroundY)
        {
            if (!bird.IsInvulnerable && !Hurt()) return;
            bird.BounceOffGround();
            return;
        }
        if (c.y + r >= GameConfig.CeilingY)
        {
            if (!bird.IsInvulnerable && !Hurt()) return;
            bird.BounceOffCeiling();
            return;
        }

        // Con súper poder, los enemigos que toca salen volando
        if (SuperOn)
        {
            foreach (var e in enemies)
                if (e.gameObject.activeSelf && (e.Position - c).sqrMagnitude < Sqr(r + e.Radius + 0.4f))
                    DestroyEnemy(e, true);
        }

        if (bird.IsInvulnerable) return;

        if (!bird.IsGhost)
        {
            foreach (var pipe in pipes)
            {
                if (pipe.gameObject.activeSelf && pipe.IsSolid && pipe.Overlaps(c, r))
                {
                    Hurt();
                    return;
                }
            }
        }

        foreach (var e in enemies)
        {
            if (e.gameObject.activeSelf && (e.Position - c).sqrMagnitude < Sqr(r + e.Radius * GameConfig.EnemyHitboxScale))
            {
                DestroyEnemy(e, false);
                Hurt();
                return;
            }
        }
    }

    // Golpe a Pico. Devuelve true si sigue vivo.
    bool Hurt()
    {
        if (bird.HasShield)
        {
            bird.BreakShield();
            bird.StartInvulnerability(1f);
            fx.Burst(bird.Position, 16, new Color(0.5f, 0.85f, 1f), 4f, 0.5f, 0.15f);
            PlaySfx(Sfx.ShieldBreak);
            Shake(0.3f);
            AddPopup(bird.Position, "¡Escudo roto!", new Color(0.5f, 0.85f, 1f));
            return true;
        }

        Lives--;
        Shake(0.6f);
        Vibrate();
        PlaySfx(Sfx.Hit);
        fx.Burst(bird.Position, 10, Characters.HurtColorA(character), 3f, 0.9f, 0.15f, -3f);
        fx.Burst(bird.Position, 6, Characters.HurtColorB(character), 2.5f, 0.9f, 0.12f, -3f);
        if (Lives <= 0)
        {
            EndGame();
            return false;
        }
        bird.StartInvulnerability(GameConfig.InvulnerableTime);
        return true;
    }

    void Collect(PowerUp p)
    {
        p.gameObject.SetActive(false);
        powerUpsTaken++;
        var col = PowerUp.TypeColor(p.Type);
        fx.Burst(p.Position, 14, col, 3.5f, 0.6f, 0.12f);
        fx.Burst(p.Position, 1, new Color(col.r, col.g, col.b, 0.6f), 0f, 0.35f, 0.5f, 0f, true, true, 14, 2.5f);
        AddPopup(p.Position, PowerUp.DisplayName(p.Type), col);
        switch (p.Type)
        {
            case PowerUpType.ExtraLife:
                Lives = Mathf.Min(Lives + 1, GameConfig.MaxLives);
                PlaySfx(Sfx.PowerLife);
                break;
            case PowerUpType.Gun:
                bird.AddAmmo(GameConfig.GunShots);
                PlaySfx(Sfx.PowerGun);
                break;
            case PowerUpType.Shrink:
                bird.ApplyTemp(TempPower.Shrink, GameConfig.ShrinkDuration);
                PlaySfx(Sfx.PowerShrink);
                break;
            case PowerUpType.Ghost:
                bird.ApplyTemp(TempPower.Ghost, GameConfig.GhostDuration);
                PlaySfx(Sfx.PowerGhost);
                break;
            case PowerUpType.Shield:
                bird.GiveShield();
                PlaySfx(Sfx.PowerShield);
                break;
            case PowerUpType.Special:
                ActivateSuper();
                break;
        }
    }

    void Shoot()
    {
        if (State != GameState.Playing || shotCooldown > 0f) return;
        if (!bird.UseAmmo())
        {
            PlaySfx(Sfx.Click, 0.6f, 0.7f);
            return;
        }
        shotCooldown = GameConfig.ShotCooldown;
        var tip = bird.GunTip;
        Get(projectiles, Projectile.Create).Launch(ProjectileKind.PlayerShot, tip, AimDirection(tip) * GameConfig.ShotSpeed);
        fx.Burst(tip, 5, new Color(1f, 0.9f, 0.4f), 2f, 0.15f, 0.1f);
        PlaySfx(Sfx.Shoot);
    }

    // Asistencia de puntería: si hay un enemigo adelante dentro de un cono, el disparo va hacia él.
    Vector2 AimDirection(Vector2 from)
    {
        Vector2 dir = new Vector2(1f, 0f);
        float best = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.gameObject.activeSelf) continue;
            Vector2 to = e.Position - from;
            if (to.x < 0.5f || to.x > HalfWidth * 2f) continue;
            float angle = Mathf.Abs(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
            float dist = to.sqrMagnitude;
            if (angle <= GameConfig.ShotAimAssistDegrees && dist < best)
            {
                best = dist;
                dir = to.normalized;
            }
        }
        return dir;
    }

    public void SpawnEnemyProjectile(ProjectileKind kind, Vector2 pos, Vector2 vel)
    {
        Get(projectiles, Projectile.Create).Launch(kind, pos, vel);
    }

    public void PlaySfx(Sfx sfx, float volume = 1f, float pitch = 1f)
    {
        audioManager.Play(sfx, volume, pitch);
    }

    void AddScore(int points, Vector2? at)
    {
        int before = Score;
        Score += points;
        if (at.HasValue) AddPopup(at.Value, "+" + points, new Color(1f, 0.95f, 0.4f));
        OnScoreChanged(before);
    }

    // Desbloqueos por puntuación: arma, enemigos nuevos y cambio de escenario.
    void OnScoreChanged(int before)
    {
        if (before < GameConfig.GunUnlockScore && Score >= GameConfig.GunUnlockScore)
        {
            gunGiftPending = true;
            pipesUntilPowerUp = Mathf.Min(pipesUntilPowerUp, 1);
        }

        int shouldUnlock = Score < GameConfig.FirstEnemyScore ? 0
            : Mathf.Min(4, 1 + (Score - GameConfig.FirstEnemyScore) / GameConfig.EnemyUnlockStep);
        if (shouldUnlock > unlockedEnemies)
        {
            unlockedEnemies = shouldUnlock;
            var type = (EnemyType)(unlockedEnemies - 1);
            forcedEnemy = type;
            ShowBanner("¡Nuevo enemigo: " + Enemy.DisplayName(type) + "!", Enemy.Hint(type), new Color(1f, 0.45f, 0.4f));
            PlaySfx(Sfx.EnemyWarn);
        }

        var zone = (ZoneId)(((int)startZone + Score / GameConfig.PointsPerZone) % 3);
        if (zone != currentZone && zoneTransition <= 0f)
        {
            pendingZone = zone;
            zoneTransition = 1.2f;
            zoneSwapped = false;
        }
    }

    void TickZoneTransition(float dt)
    {
        if (zoneTransition <= 0f) return;
        zoneTransition -= dt;
        if (!zoneSwapped && zoneTransition <= 0.6f)
        {
            zoneSwapped = true;
            currentZone = pendingZone;
            zonesVisited++;
            scenery.Build(currentZone);
            foreach (var p in pipes) if (p.gameObject.activeSelf) p.ApplyTheme(Theme);
            audioManager.PlaySong(Theme.song);
            PlaySfx(Sfx.Zone);
            ShowBanner("Zona " + zonesVisited + ": " + Theme.name, "La aventura continúa", Theme.uiAccent);
        }
    }

    // ---------------------------------------------------------------- Generación

    void SpawnPipe(float x)
    {
        float gap = Mathf.Lerp(GameConfig.StartGap, GameConfig.MinGap, Difficulty01);
        float minC = GameConfig.GroundY + gap * 0.5f + 0.6f;
        float maxC = GameConfig.CeilingY - gap * 0.5f - 0.6f;
        float center = Random.Range(minC, maxC);
        // Evita saltos imposibles entre un hueco y el siguiente
        center = Mathf.Clamp(center, lastGapCenter - GameConfig.MaxGapCenterJump, lastGapCenter + GameConfig.MaxGapCenterJump);
        lastGapCenter = center;

        var newPipe = Get(pipes, PipePair.Create);
        newPipe.Setup(x, center, gap, ChoosePipeKind(), Theme);
        OnPipeSpawned(newPipe);

        bool spawnedPowerUp = false;
        pipesUntilPowerUp--;
        if (ShouldSpawnSpecial())
        {
            Get(powerUps, PowerUp.Create).Spawn(PowerUpType.Special, new Vector2(x + GameConfig.PipeSpacing * 0.5f, center));
            spawnedPowerUp = true;
        }
        else if (pipesUntilPowerUp <= 0)
        {
            pipesUntilPowerUp = Random.Range(GameConfig.PowerUpMinPipes, GameConfig.PowerUpMaxPipes + 1);
            Get(powerUps, PowerUp.Create).Spawn(ChoosePowerUp(), new Vector2(x + GameConfig.PipeSpacing * 0.5f, center));
            spawnedPowerUp = true;
        }

        TrySpawnEnemy(x + GameConfig.PipeSpacing * 0.5f, spawnedPowerUp);
    }

    PipeKind ChoosePipeKind()
    {
        float t = Mathf.InverseLerp(GameConfig.CrackedPipesScore, GameConfig.MasteryScore, Score);
        float chance = Mathf.Lerp(0.12f, GameConfig.SpecialPipeChanceMax, t);
        if (Score >= GameConfig.CrackedPipesScore && Random.value < chance)
            return Score >= GameConfig.GhostPipesScore && Random.value < 0.5f ? PipeKind.Ghost : PipeKind.Cracked;
        return PipeKind.Normal;
    }

    PowerUpType ChoosePowerUp()
    {
        if (gunGiftPending)
        {
            gunGiftPending = false;
            return PowerUpType.Gun;
        }
        var options = new List<PowerUpType>();
        var weights = new List<float>();
        System.Action<PowerUpType, float> Option = (t, w) => { if (w > 0f) { options.Add(t); weights.Add(w); } };

        Option(PowerUpType.ExtraLife, Lives < GameConfig.MaxLives ? 2f : 0f);
        Option(PowerUpType.Shrink, 1.5f);
        Option(PowerUpType.Gun, Score >= GameConfig.GunUnlockScore ? (unlockedEnemies > 0 ? 3f : 1.5f) : 0f);
        Option(PowerUpType.Ghost, Score >= GameConfig.GhostUnlockScore ? 1.5f : 0f);
        Option(PowerUpType.Shield, Score >= GameConfig.ShieldUnlockScore && !bird.HasShield ? 1.5f : 0f);

        float total = 0f;
        foreach (var w in weights) total += w;
        float pick = Random.value * total;
        for (int i = 0; i < options.Count; i++)
        {
            pick -= weights[i];
            if (pick <= 0f) return options[i];
        }
        return options[options.Count - 1];
    }

    // Los enemigos empiezan en 10 puntos y su frecuencia sube poco a poco.
    // Al principio solo puede haber uno en pantalla y con descanso entre uno y otro.
    void TrySpawnEnemy(float x, bool powerUpHere)
    {
        if (unlockedEnemies == 0 || SuperOn) return;
        pipesSinceEnemy++;

        int active = 0;
        foreach (var e in enemies) if (e.gameObject.activeSelf) active++;
        int maxActive = Score < 40 ? 1 : GameConfig.MaxEnemiesOnScreen;
        int minGap = Score < 30 ? 2 : 1;
        float chance = Mathf.Min(GameConfig.EnemyChanceMax,
            GameConfig.EnemyChanceStart + (Score - GameConfig.FirstEnemyScore) * GameConfig.EnemyChancePerPoint);

        EnemyType type;
        if (forcedEnemy.HasValue)
        {
            if (active >= maxActive) return; // espera a que se vaya el anterior
            type = forcedEnemy.Value;
            forcedEnemy = null;
        }
        else
        {
            if (powerUpHere || active >= maxActive || pipesSinceEnemy <= minGap || Random.value > chance) return;
            type = (EnemyType)Random.Range(0, unlockedEnemies);
        }
        pipesSinceEnemy = 0;

        float y;
        switch (type)
        {
            case EnemyType.Wasp: y = Random.Range(-1.5f, 1.5f); break;
            case EnemyType.Drone: y = Random.Range(-2.3f, 2.3f); break;
            case EnemyType.Crow: y = GameConfig.CeilingY - 1.1f; break;
            default: y = Random.Range(GameConfig.GroundY + 1.3f, GameConfig.CeilingY - 1.3f); break;
        }
        float attackSlow = Mathf.Lerp(GameConfig.EnemyAttackSlowStart, 1f,
            Mathf.InverseLerp(GameConfig.FirstEnemyScore, GameConfig.MasteryScore, Score));
        Get(enemies, Enemy.Create).Spawn(type, new Vector2(x, y), attackSlow);
    }

    // Pooling: reutiliza un objeto inactivo o crea uno nuevo.
    T Get<T>(List<T> pool, System.Func<Transform, T> create) where T : Component
    {
        foreach (var item in pool)
            if (!item.gameObject.activeSelf) return item;
        var created = create(world);
        pool.Add(created);
        return created;
    }

    static float Sqr(float v) => v * v;

    void Shake(float amount) { shake = Mathf.Max(shake, amount); }

    void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (vibration) Handheld.Vibrate();
#endif
    }
}
