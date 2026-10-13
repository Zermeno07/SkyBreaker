using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver }

// Administrador del juego: estados (menú, jugando, pausa, fin de partida), generación de
// tuberías/poderes/enemigos con pooling, colisiones, puntuación, récord y la interfaz (HUD).
//
// La entrada se lee con los eventos de IMGUI (OnGUI), así funciona igual con el
// Input Manager clásico y con el nuevo Input System, sin instalar paquetes.
public class GameManager : MonoBehaviour
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
    BackgroundScroller background;

    readonly List<PipePair> pipes = new List<PipePair>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<PowerUp> powerUps = new List<PowerUp>();

    float distanceSinceLastPipe;
    float lastGapCenter;
    int pipesUntilPowerUp;
    bool newRecord;
    float gameOverTime;
    bool flapKeyHeld;

    static readonly Color[] SkyColors =
    {
        new Color(1f, 0.72f, 0.55f),   // amanecer
        new Color(0.45f, 0.75f, 1f),   // día
        new Color(1f, 0.5f, 0.38f),    // atardecer
        new Color(0.1f, 0.12f, 0.28f), // noche
    };

    float HalfWidth => cam.orthographicSize * cam.aspect;
    float BirdX => -Mathf.Min(HalfWidth * 0.45f, 4f);
    float Difficulty01 => Mathf.Clamp01(Score / (float)GameConfig.MasteryScore);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;

        SetupCamera();
        world = new GameObject("Mundo").transform;
        background = new BackgroundScroller(world);
        bird = PlayerBird.Create(world);
        HighScore = PlayerPrefs.GetInt(GameConfig.HighScoreKey, 0);
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
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = SkyColors[0];
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
    }

    // ---------------------------------------------------------------- Estados

    void GoToMenu()
    {
        Time.timeScale = 1f;
        ClearWorld();
        Score = 0;
        bird.ResetBird(BirdX);
        State = GameState.Menu;
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
        bird.ResetBird(BirdX);
        bird.Flap();
        State = GameState.Playing;
    }

    void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
    }

    void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    void EndGame()
    {
        State = GameState.GameOver;
        gameOverTime = Time.unscaledTime;
        if (Score > HighScore)
        {
            HighScore = Score;
            newRecord = true;
            PlayerPrefs.SetInt(GameConfig.HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }
    }

    void ClearWorld()
    {
        foreach (var p in pipes) p.gameObject.SetActive(false);
        foreach (var e in enemies) e.gameObject.SetActive(false);
        foreach (var p in powerUps) p.gameObject.SetActive(false);
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
                background.Tick(GameConfig.StartSpeed * 0.5f * dt);
                break;
            case GameState.Playing:
                TickPlaying(dt);
                break;
            case GameState.GameOver:
                bird.TickDead(dt);
                break;
        }
        UpdateSkyColor(dt);
    }

    void TickPlaying(float dt)
    {
        Speed = Mathf.Lerp(GameConfig.StartSpeed, GameConfig.MaxSpeed, Difficulty01);
        float dx = Speed * dt;
        float halfW = HalfWidth;

        bird.TickPhysics(dt);
        background.Tick(dx);

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
            pipe.Move(dx);
            if (!pipe.Scored && pipe.X + pipe.HalfWidth < c.x - r)
            {
                pipe.Scored = true;
                Score += 1;
            }
            if (pipe.X < -halfW - GameConfig.PipeWidth * 2f) pipe.gameObject.SetActive(false);
        }

        foreach (var e in enemies)
        {
            if (!e.gameObject.activeSelf) continue;
            e.Tick(dx + GameConfig.EnemyExtraSpeed * dt);
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

        CheckCollisions(c, r);
    }

    void CheckCollisions(Vector2 c, float r)
    {
        // El suelo y el techo siempre bloquean; quitan vida si no hay invulnerabilidad.
        if (c.y - r <= GameConfig.GroundY)
        {
            if (!bird.IsInvulnerable && !TakeHit()) return;
            bird.BounceOffGround();
            return;
        }
        if (c.y + r >= GameConfig.CeilingY)
        {
            if (!bird.IsInvulnerable && !TakeHit()) return;
            bird.BounceOffCeiling();
            return;
        }

        if (bird.IsInvulnerable) return;

        foreach (var pipe in pipes)
        {
            if (pipe.gameObject.activeSelf && pipe.Overlaps(c, r))
            {
                TakeHit();
                return;
            }
        }

        foreach (var e in enemies)
        {
            if (e.gameObject.activeSelf && (e.Position - c).sqrMagnitude < Sqr(r + GameConfig.EnemyRadius))
            {
                e.gameObject.SetActive(false);
                TakeHit();
                return;
            }
        }
    }

    // Devuelve true si Pico sigue vivo después del golpe.
    bool TakeHit()
    {
        Lives--;
        if (Lives <= 0)
        {
            EndGame();
            return false;
        }
        bird.StartInvulnerability();
        return true;
    }

    void Collect(PowerUp p)
    {
        p.gameObject.SetActive(false);
        switch (p.Type)
        {
            case PowerUpType.ExtraLife:
                Lives = Mathf.Min(Lives + 1, GameConfig.MaxLives);
                break;
            case PowerUpType.Shrink:
                bird.ApplyShrink();
                break;
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

        Get(pipes, PipePair.Create).Setup(x, center, gap);

        pipesUntilPowerUp--;
        if (pipesUntilPowerUp <= 0)
        {
            pipesUntilPowerUp = Random.Range(GameConfig.PowerUpMinPipes, GameConfig.PowerUpMaxPipes + 1);
            var type = Lives < GameConfig.MaxLives && Random.value < 0.5f ? PowerUpType.ExtraLife : PowerUpType.Shrink;
            Get(powerUps, PowerUp.Create).Spawn(type, new Vector2(x + GameConfig.PipeSpacing * 0.5f, center));
        }

        if (Score >= GameConfig.EnemyStartScore)
        {
            float t = Mathf.InverseLerp(GameConfig.EnemyStartScore, GameConfig.MasteryScore, Score);
            if (Random.value < Mathf.Lerp(GameConfig.EnemyChanceStart, GameConfig.EnemyChanceMax, t))
            {
                float y = Random.Range(GameConfig.GroundY + 1f, GameConfig.CeilingY - 1f);
                Get(enemies, Enemy.Create).Spawn(new Vector2(x + GameConfig.PipeSpacing * 0.5f, y));
            }
        }
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

    void UpdateSkyColor(float dt)
    {
        float t = Score / 25f;
        int i = Mathf.FloorToInt(t) % SkyColors.Length;
        var target = Color.Lerp(SkyColors[i], SkyColors[(i + 1) % SkyColors.Length], t - Mathf.Floor(t));
        cam.backgroundColor = Color.Lerp(cam.backgroundColor, target, Mathf.Clamp01(dt * 2f));
    }

    // ---------------------------------------------------------------- Interfaz (IMGUI)

    GUIStyle big, medium, small, button;
    float uiScale;

    void EnsureStyles()
    {
        uiScale = Mathf.Max(0.5f, Mathf.Min(Screen.width, Screen.height * 1.4f) / 1000f);
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            medium = new GUIStyle(big);
            small = new GUIStyle(big) { fontStyle = FontStyle.Normal };
            button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
        }
        big.fontSize = Mathf.RoundToInt(80 * uiScale);
        medium.fontSize = Mathf.RoundToInt(40 * uiScale);
        small.fontSize = Mathf.RoundToInt(26 * uiScale);
        button.fontSize = Mathf.RoundToInt(34 * uiScale);
    }

    void OnGUI()
    {
        EnsureStyles();
        Rect safe = Screen.safeArea;
        safe.y = Screen.height - safe.yMax; // safeArea usa origen abajo; IMGUI usa origen arriba

        switch (State)
        {
            case GameState.Menu: DrawMenu(safe); break;
            case GameState.Playing: DrawHud(safe); break;
            case GameState.Paused: DrawHud(safe); DrawPause(); break;
            case GameState.GameOver: DrawHud(safe); DrawGameOver(); break;
        }

        // Los botones de arriba consumen su clic, así que aquí solo llegan toques "libres".
        HandleInput(Event.current);
    }

    void HandleInput(Event e)
    {
        bool flapKey = e.keyCode == KeyCode.Space || e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W;
        if (e.type == EventType.KeyUp && flapKey) flapKeyHeld = false;

        bool press = e.type == EventType.MouseDown && e.button == 0;
        if (e.type == EventType.KeyDown && flapKey && !flapKeyHeld)
        {
            flapKeyHeld = true;
            press = true;
        }
        bool escape = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.P);

        if (!press && !escape) return;
        e.Use();

        switch (State)
        {
            case GameState.Menu:
                if (press) StartGame();
                break;
            case GameState.Playing:
                if (escape) Pause();
                else bird.Flap();
                break;
            case GameState.Paused:
                if (escape) Resume();
                break;
            case GameState.GameOver:
                if (escape) GoToMenu();
                else if (Time.unscaledTime - gameOverTime > 0.6f) StartGame();
                break;
        }
    }

    void DrawMenu(Rect safe)
    {
        float w = Screen.width, h = Screen.height;
        ShadowLabel(new Rect(0, h * 0.14f, w, 110 * uiScale), "SKY BREAKER", big, Color.white);
        ShadowLabel(new Rect(0, h * 0.14f + 100 * uiScale, w, 50 * uiScale), "Prototipo jugable - avance 50%", small, new Color(1f, 1f, 0.8f));
        ShadowLabel(new Rect(0, h * 0.40f, w, 60 * uiScale), "Récord: " + HighScore, medium, Color.white);

        if (CenterButton(h * 0.52f, "JUGAR")) StartGame();

        ShadowLabel(new Rect(0, h - safe.y - 160 * uiScale, w, 40 * uiScale),
            "Toca la pantalla, haz clic o presiona ESPACIO para aletear", small, Color.white);
        ShadowLabel(new Rect(0, h - safe.y - 120 * uiScale, w, 40 * uiScale),
            "ESC o P para pausar", small, Color.white);

        var r = new Rect(safe.xMax - 260 * uiScale, h - safe.y - 70 * uiScale, 240 * uiScale, 50 * uiScale);
        if (GUI.Button(r, "Borrar récord", button))
        {
            HighScore = 0;
            PlayerPrefs.DeleteKey(GameConfig.HighScoreKey);
        }
    }

    void DrawHud(Rect safe)
    {
        float w = Screen.width;
        float top = safe.y + 10 * uiScale;

        ShadowLabel(new Rect(0, top, w, 100 * uiScale), Score.ToString(), big, Color.white);
        ShadowLabel(new Rect(0, top + 85 * uiScale, w, 40 * uiScale), "Récord: " + HighScore, small, Color.white);

        // Vidas (corazones) arriba a la izquierda
        float hs = 50 * uiScale;
        for (int i = 0; i < GameConfig.MaxLives; i++)
        {
            var col = i < Lives ? new Color(0.95f, 0.15f, 0.25f) : new Color(0f, 0f, 0f, 0.3f);
            DrawHeart(new Vector2(safe.x + 40 * uiScale + i * hs * 1.15f, top + 35 * uiScale), hs, col);
        }

        // Poder temporal activo con su barra de tiempo
        if (bird.IsShrunk && State != GameState.GameOver)
        {
            float x = safe.x + 20 * uiScale;
            float y = top + 75 * uiScale;
            ShadowLabel(new Rect(x, y, 200 * uiScale, 36 * uiScale), "ENCOGIDO", small, new Color(0.85f, 0.7f, 1f));
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(x, y + 36 * uiScale, 200 * uiScale, 12 * uiScale), Texture2D.whiteTexture);
            GUI.color = new Color(0.7f, 0.4f, 1f);
            GUI.DrawTexture(new Rect(x, y + 36 * uiScale, 200 * uiScale * bird.ShrinkRemaining01, 12 * uiScale), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (State == GameState.Playing)
        {
            float s = 80 * uiScale;
            if (GUI.Button(new Rect(safe.xMax - s - 20 * uiScale, top, s, s), "II", button)) Pause();
        }
    }

    void DrawPause()
    {
        DrawDim();
        float h = Screen.height;
        ShadowLabel(new Rect(0, h * 0.22f, Screen.width, 100 * uiScale), "PAUSA", big, Color.white);
        if (CenterButton(h * 0.42f, "CONTINUAR")) Resume();
        if (CenterButton(h * 0.42f + 100 * uiScale, "REINICIAR")) StartGame();
        if (CenterButton(h * 0.42f + 200 * uiScale, "MENÚ PRINCIPAL")) GoToMenu();
    }

    void DrawGameOver()
    {
        DrawDim();
        float w = Screen.width, h = Screen.height;
        ShadowLabel(new Rect(0, h * 0.16f, w, 100 * uiScale), "FIN DE LA PARTIDA", big, Color.white);
        ShadowLabel(new Rect(0, h * 0.16f + 110 * uiScale, w, 60 * uiScale), "Puntuación: " + Score, medium, Color.white);
        ShadowLabel(new Rect(0, h * 0.16f + 165 * uiScale, w, 60 * uiScale), "Récord: " + HighScore, medium, Color.white);
        if (newRecord)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), new Vector2(w * 0.5f, h * 0.16f + 250 * uiScale));
            ShadowLabel(new Rect(0, h * 0.16f + 220 * uiScale, w, 60 * uiScale), "¡NUEVO RÉCORD!", medium, new Color(1f, 0.85f, 0.2f));
            GUI.matrix = m;
        }
        if (CenterButton(h * 0.16f + 300 * uiScale, "REINTENTAR")) StartGame();
        if (CenterButton(h * 0.16f + 400 * uiScale, "MENÚ PRINCIPAL")) GoToMenu();
    }

    bool CenterButton(float y, string text)
    {
        float bw = 380 * uiScale, bh = 80 * uiScale;
        return GUI.Button(new Rect((Screen.width - bw) * 0.5f, y, bw, bh), text, button);
    }

    void DrawDim()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void ShadowLabel(Rect r, string text, GUIStyle style, Color color)
    {
        float o = Mathf.Max(1f, 3f * uiScale);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
    }

    void DrawHeart(Vector2 center, float size, Color color)
    {
        var tex = SpriteFactory.Circle.texture;
        GUI.color = color;
        float d = size * 0.55f;
        GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.35f, d, d), tex);
        GUI.DrawTexture(new Rect(center.x - size * 0.05f, center.y - size * 0.35f, d, d), tex);
        var m = GUI.matrix;
        var pivot = new Vector2(center.x, center.y + size * 0.05f);
        GUIUtility.RotateAroundPivot(45f, pivot);
        float sq = size * 0.5f;
        GUI.DrawTexture(new Rect(pivot.x - sq * 0.5f, pivot.y - sq * 0.5f, sq, sq), Texture2D.whiteTexture);
        GUI.matrix = m;
        GUI.color = Color.white;
    }
}
