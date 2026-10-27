using System.Collections.Generic;
using UnityEngine;

// Interfaz del juego con IMGUI: menú, HUD, pausa, ajustes, créditos y fin de partida.
// La entrada también se lee aquí (eventos de IMGUI), así funciona igual con el
// Input Manager clásico y con el nuevo Input System, sin instalar paquetes.
public partial class GameManager
{
    enum MenuScreen { Main, Settings, Credits }

    MenuScreen menuScreen;
    MenuScreen pauseScreen;
    bool vibration;
    bool confirmReset;
    bool flapKeyHeld;

    struct Popup
    {
        public Vector2 world;
        public string text;
        public Color color;
        public float t;
    }

    readonly List<Popup> popups = new List<Popup>();
    string bannerTitle, bannerSub;
    Color bannerColor;
    float bannerTimer;
    const float BannerTime = 2.8f;

    GUIStyle big, medium, small, tiny, buttonStyle, buttonText;
    Texture2D btnNormal, btnHover, btnActive, panelTex;
    float uiScale;
    Rect shootRect;

    // ---------------------------------------------------------------- Avisos

    void ShowBanner(string title, string sub, Color color)
    {
        bannerTitle = title;
        bannerSub = sub;
        bannerColor = color;
        bannerTimer = BannerTime;
    }

    void AddPopup(Vector2 world, string text, Color color)
    {
        popups.Add(new Popup { world = world, text = text, color = color, t = 0f });
    }

    void TickPopups(float dt)
    {
        bannerTimer -= dt;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.t += dt;
            p.world.y += dt * 1.2f;
            if (State == GameState.Playing) p.world.x -= Speed * dt;
            if (p.t > 1.1f) popups.RemoveAt(i);
            else popups[i] = p;
        }
    }

    // ---------------------------------------------------------------- Estilos

    static Texture2D RoundedTex(Color fill, Color border, Color highlight)
    {
        const int s = 12;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool corner = (x == 0 || x == s - 1) && (y == 0 || y == s - 1);
                Color c;
                if (corner) c = Color.clear;
                else if (x == 0 || y == 0 || x == s - 1 || y == s - 1) c = border;
                else if (y >= s - 3) c = highlight;
                else if (y <= 2) c = Color.Lerp(fill, border, 0.35f);
                else c = fill;
                px[y * s + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    void EnsureStyles()
    {
        uiScale = Mathf.Max(0.5f, Mathf.Min(Screen.width, Screen.height * 1.4f) / 1000f);
        if (big == null)
        {
            var border = ZoneTheme.Hex(0x1a1c2c);
            btnNormal = RoundedTex(ZoneTheme.Hex(0xffb83f), border, ZoneTheme.Hex(0xffe08a));
            btnHover = RoundedTex(ZoneTheme.Hex(0xffcc5c), border, ZoneTheme.Hex(0xfff0b8));
            btnActive = RoundedTex(ZoneTheme.Hex(0xe0902a), border, ZoneTheme.Hex(0xffb83f));
            panelTex = RoundedTex(new Color(0.08f, 0.09f, 0.18f, 0.88f), ZoneTheme.Hex(0x1a1c2c), new Color(0.2f, 0.22f, 0.4f, 0.9f));

            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
            medium = new GUIStyle(big);
            small = new GUIStyle(big);
            tiny = new GUIStyle(big) { fontStyle = FontStyle.Normal };
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = btnNormal;
            buttonStyle.hover.background = btnHover;
            buttonStyle.active.background = btnActive;
            buttonStyle.focused.background = btnNormal;
            buttonStyle.border = new RectOffset(4, 4, 4, 4);
            buttonText = new GUIStyle(big);
        }
        big.fontSize = Mathf.RoundToInt(84 * uiScale);
        medium.fontSize = Mathf.RoundToInt(42 * uiScale);
        small.fontSize = Mathf.RoundToInt(30 * uiScale);
        tiny.fontSize = Mathf.RoundToInt(24 * uiScale);
        buttonText.fontSize = Mathf.RoundToInt(34 * uiScale);
    }

    // ---------------------------------------------------------------- Dibujo principal

    void OnGUI()
    {
        EnsureStyles();
        Rect safe = Screen.safeArea;
        safe.y = Screen.height - safe.yMax; // safeArea usa origen abajo; IMGUI usa origen arriba

        DrawTripOverlay();
        DrawPopups();

        // Destellos: relámpagos y cambio de escenario
        float flash = scenery.Flash * 0.45f;
        if (zoneTransition > 0f) flash = Mathf.Max(flash, 1f - Mathf.Abs(zoneTransition - 0.6f) / 0.6f);
        if (flash > 0.01f)
        {
            GUI.color = new Color(1f, 1f, 1f, flash);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        switch (State)
        {
            case GameState.Menu:
                if (menuScreen == MenuScreen.Settings) DrawSettings(() => menuScreen = MenuScreen.Main);
                else if (menuScreen == MenuScreen.Credits) DrawCredits();
                else DrawMenu(safe);
                break;
            case GameState.Playing:
                DrawHud(safe);
                break;
            case GameState.Paused:
                DrawHud(safe);
                if (pauseScreen == MenuScreen.Settings) DrawSettings(() => pauseScreen = MenuScreen.Main);
                else DrawPause();
                break;
            case GameState.GameOver:
                DrawHud(safe);
                DrawGameOver();
                break;
        }

        DrawBanner(safe);

        // Los botones de arriba consumen su clic, así que aquí solo llegan toques "libres".
        HandleInput(Event.current);
    }

    void HandleInput(Event e)
    {
        bool flapKey = e.keyCode == KeyCode.Space || e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W;
        bool shootKey = e.keyCode == KeyCode.X || e.keyCode == KeyCode.F || e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.LeftShift;
        if (e.type == EventType.KeyUp && flapKey) flapKeyHeld = false;

        bool press = e.type == EventType.MouseDown && e.button == 0;
        bool shoot = (e.type == EventType.MouseDown && e.button == 1) || (e.type == EventType.KeyDown && shootKey);
        if (e.type == EventType.KeyDown && flapKey && !flapKeyHeld)
        {
            flapKeyHeld = true;
            press = true;
        }
        bool escape = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.P);

        // Botón táctil de disparo
        if (press && e.type == EventType.MouseDown && State == GameState.Playing && bird.Ammo > 0 && shootRect.Contains(e.mousePosition))
        {
            press = false;
            shoot = true;
        }

        if (!press && !escape && !shoot) return;
        e.Use();

        switch (State)
        {
            case GameState.Menu:
                if (press && menuScreen == MenuScreen.Main) StartGame();
                else if (escape) menuScreen = MenuScreen.Main;
                break;
            case GameState.Playing:
                if (escape) Pause();
                else if (shoot) Shoot();
                else
                {
                    bird.Flap();
                    if (Characters.IsHead(character)) PlaySfx(Sfx.Boing, 0.4f, Random.Range(0.95f, 1.1f));
                    else PlaySfx(Sfx.Flap, 0.5f, Random.Range(0.95f, 1.08f));
                    fx.Burst(bird.Position + new Vector2(-0.3f, -0.1f), 2, new Color(1f, 1f, 1f, 0.6f), 0.8f, 0.3f, 0.12f, 0f, true, true, 9);
                }
                break;
            case GameState.Paused:
                if (escape) { if (pauseScreen == MenuScreen.Main) Resume(); else pauseScreen = MenuScreen.Main; }
                break;
            case GameState.GameOver:
                if (escape) GoToMenu();
                else if (press && Time.unscaledTime - gameOverTime > 0.8f) StartGame();
                break;
        }
    }

    // ---------------------------------------------------------------- Menú

    void DrawMenu(Rect safe)
    {
        float w = Screen.width, h = Screen.height, u = uiScale;
        float top = safe.y + 20 * u;
        float bob = Mathf.Sin(Time.unscaledTime * 2f) * 8f * u;
        OutlineLabel(new Rect(0, top + bob, w, 120 * u), "SKY BREAKER", big, ZoneTheme.Hex(0xffd23f), 5f);
        OutlineLabel(new Rect(0, top + 95 * u + bob, w, 45 * u), "Prototipo jugable · avance 75%", tiny, Color.white, 2f);
        OutlineLabel(new Rect(0, top + 140 * u, w, 50 * u), "Récord: " + HighScore, medium, Color.white, 3f);

        float y = top + 200 * u;
        if (CenterButton(y, "JUGAR", 1.1f)) { PlaySfx(Sfx.Click); StartGame(); }

        // Selectores de personaje y de escenario: lado a lado si caben, si no uno debajo del otro
        y += 110 * u;
        float selW = 470 * u, selH = 90 * u, gapX = 24 * u;
        bool sideBySide = selW * 2 + gapX <= w - 20 * u;
        var charRect = sideBySide ? new Rect(w * 0.5f - selW - gapX * 0.5f, y, selW, selH) : new Rect((w - selW) * 0.5f, y, selW, selH);
        var zoneRect = sideBySide ? new Rect(w * 0.5f + gapX * 0.5f, y, selW, selH) : new Rect((w - selW) * 0.5f, y + selH + 15 * u, selW, selH);

        int dc = Selector(charRect, "Personaje", Characters.Name(character), Color.white, Characters.Preview(character).texture);
        if (dc != 0) ChangeCharacter(dc);
        var zone = ZoneTheme.All[(int)startZone];
        int dz = Selector(zoneRect, "Escenario inicial", zone.name, zone.uiAccent, null);
        if (dz != 0) ChangeStartZone(dz);

        y = Mathf.Max(charRect.yMax, zoneRect.yMax) + 20 * u;
        float bw = 260 * u, bh = 70 * u;
        if (Button(new Rect(w * 0.5f - bw - 10 * u, y, bw, bh), "AJUSTES")) { PlaySfx(Sfx.Click); menuScreen = MenuScreen.Settings; confirmReset = false; }
        if (Button(new Rect(w * 0.5f + 10 * u, y, bw, bh), "CRÉDITOS")) { PlaySfx(Sfx.Click); menuScreen = MenuScreen.Credits; }

        OutlineLabel(new Rect(0, h - safe.y - 90 * u, w, 40 * u),
            "Aletear: toque, clic o ESPACIO   ·   Disparar: X, F o clic derecho", tiny, Color.white, 2f);
        OutlineLabel(new Rect(0, h - safe.y - 55 * u, w, 40 * u),
            "Pausa: ESC o P", tiny, Color.white, 2f);
    }

    // Selector con flechas a los lados. Devuelve -1, 0 o 1 según la flecha presionada.
    int Selector(Rect r, string label, string value, Color valueColor, Texture icon)
    {
        float aw = 64 * uiScale;
        int result = 0;
        if (Button(new Rect(r.x, r.y + (r.height - aw) * 0.5f, aw, aw), "<")) result = -1;
        if (Button(new Rect(r.xMax - aw, r.y + (r.height - aw) * 0.5f, aw, aw), ">")) result = 1;
        var mid = new Rect(r.x + aw + 6 * uiScale, r.y, r.width - aw * 2 - 12 * uiScale, r.height);
        DrawPanel(mid);
        var text = mid;
        if (icon != null)
        {
            float isz = r.height - 16 * uiScale;
            GUI.DrawTexture(new Rect(mid.x + 8 * uiScale, mid.y + 8 * uiScale, isz, isz), icon, ScaleMode.ScaleToFit);
            text = new Rect(mid.x + isz + 8 * uiScale, mid.y, mid.width - isz - 12 * uiScale, mid.height);
        }
        OutlineLabel(new Rect(text.x, text.y + 4 * uiScale, text.width, text.height * 0.4f), label, tiny, new Color(0.8f, 0.85f, 1f), 1f);
        OutlineLabel(new Rect(text.x, text.y + text.height * 0.38f, text.width, text.height * 0.55f), value, small, valueColor, 2f);
        return result;
    }

    void ChangeStartZone(int dir)
    {
        PlaySfx(Sfx.Click);
        startZone = (ZoneId)(((int)startZone + dir + 3) % 3);
        PlayerPrefs.SetInt(GameConfig.StartZoneKey, (int)startZone);
        currentZone = startZone;
        scenery.Build(startZone);
    }

    void ChangeCharacter(int dir)
    {
        PlaySfx(Sfx.Click);
        character = (CharacterId)(((int)character + dir + Characters.Count) % Characters.Count);
        PlayerPrefs.SetInt(GameConfig.CharacterKey, (int)character);
        bird.SetCharacter(character);
        PowerUp.CurrentSuper = Characters.Super(character);
        PlaySfx(Characters.IsHead(character) ? Sfx.Boing : Sfx.Flap);
    }

    void DrawSettings(System.Action back)
    {
        float w = Screen.width, h = Screen.height;
        DrawDim(0.45f);
        var panel = new Rect((w - 700 * uiScale) * 0.5f, h * 0.12f, 700 * uiScale, 620 * uiScale);
        DrawPanel(panel);
        OutlineLabel(new Rect(panel.x, panel.y + 15 * uiScale, panel.width, 80 * uiScale), "AJUSTES", medium, ZoneTheme.Hex(0xffd23f), 3f);

        float y = panel.y + 110 * uiScale;
        float v = VolumeRow(panel, y, "Música", audioManager.MusicVolume);
        if (!Mathf.Approximately(v, audioManager.MusicVolume)) audioManager.SetMusicVolume(v);
        y += 95 * uiScale;
        v = VolumeRow(panel, y, "Efectos", audioManager.SfxVolume);
        if (!Mathf.Approximately(v, audioManager.SfxVolume)) { audioManager.SetSfxVolume(v); PlaySfx(Sfx.Score); }
        y += 95 * uiScale;

        OutlineLabel(new Rect(panel.x + 30 * uiScale, y, 260 * uiScale, 70 * uiScale), "Vibración", small, Color.white, 2f);
        if (Button(new Rect(panel.x + panel.width - 330 * uiScale, y, 300 * uiScale, 70 * uiScale), vibration ? "SÍ" : "NO"))
        {
            vibration = !vibration;
            PlayerPrefs.SetInt(GameConfig.VibrationKey, vibration ? 1 : 0);
            PlaySfx(Sfx.Click);
        }
        y += 105 * uiScale;

        string resetText = confirmReset ? "¿SEGURO? TOCA OTRA VEZ" : "BORRAR RÉCORD";
        if (Button(new Rect(panel.x + 60 * uiScale, y, panel.width - 120 * uiScale, 70 * uiScale), resetText))
        {
            PlaySfx(Sfx.Click);
            if (confirmReset)
            {
                HighScore = 0;
                PlayerPrefs.DeleteKey(GameConfig.HighScoreKey);
                confirmReset = false;
                AddPopup(Vector2.zero, "Récord borrado", Color.white);
            }
            else confirmReset = true;
        }
        y += 100 * uiScale;
        if (Button(new Rect(panel.x + 160 * uiScale, y, panel.width - 320 * uiScale, 75 * uiScale), "VOLVER"))
        {
            PlaySfx(Sfx.Click);
            PlayerPrefs.Save();
            confirmReset = false;
            back();
        }
    }

    float VolumeRow(Rect panel, float y, string label, float value)
    {
        OutlineLabel(new Rect(panel.x + 30 * uiScale, y, 200 * uiScale, 70 * uiScale), label, small, Color.white, 2f);
        float bx = panel.x + 230 * uiScale;
        if (Button(new Rect(bx, y, 70 * uiScale, 70 * uiScale), "-")) value = Mathf.Round(value * 10f - 1f) / 10f;
        var bar = new Rect(bx + 85 * uiScale, y + 18 * uiScale, 240 * uiScale, 34 * uiScale);
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = ZoneTheme.Hex(0xffd23f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(value), bar.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        OutlineLabel(bar, Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%", tiny, Color.white, 2f);
        if (Button(new Rect(bar.xMax + 15 * uiScale, y, 70 * uiScale, 70 * uiScale), "+")) value = Mathf.Round(value * 10f + 1f) / 10f;
        return Mathf.Clamp01(value);
    }

    void DrawCredits()
    {
        float w = Screen.width, h = Screen.height;
        DrawDim(0.45f);
        var panel = new Rect((w - 760 * uiScale) * 0.5f, h * 0.1f, 760 * uiScale, 640 * uiScale);
        DrawPanel(panel);
        OutlineLabel(new Rect(panel.x, panel.y + 15 * uiScale, panel.width, 80 * uiScale), "CRÉDITOS", medium, ZoneTheme.Hex(0xffd23f), 3f);
        string[] lines =
        {
            "Sky Breaker",
            "Creación de videojuegos · Grupo IDGS10D11",
            "",
            "Diseño, programación y arte: [Nombre del equipo]",
            "Personajes: Pico, Wicho L Pop's, Jorge Nitales,",
            "Justo Prime y Paleteado",
            "Motor: Unity · Lenguaje: C#",
            "Arte en pixel art y sonido chiptune",
            "generados por código para el prototipo",
            "",
            "¡Gracias por jugar!",
        };
        float y = panel.y + 105 * uiScale;
        foreach (var l in lines)
        {
            OutlineLabel(new Rect(panel.x, y, panel.width, 44 * uiScale), l, tiny, Color.white, 2f);
            y += 44 * uiScale;
        }
        if (Button(new Rect(panel.x + 200 * uiScale, panel.yMax - 100 * uiScale, panel.width - 400 * uiScale, 75 * uiScale), "VOLVER"))
        {
            PlaySfx(Sfx.Click);
            menuScreen = MenuScreen.Main;
        }
    }

    // ---------------------------------------------------------------- HUD

    void DrawHud(Rect safe)
    {
        float w = Screen.width, h = Screen.height;
        float top = safe.y + 10 * uiScale;

        OutlineLabel(new Rect(0, top, w, 100 * uiScale), Score.ToString(), big, Color.white, 5f);
        OutlineLabel(new Rect(0, top + 88 * uiScale, w, 40 * uiScale), "Récord: " + HighScore, tiny, Color.white, 2f);

        // Súper poder activo: ícono y tuberías restantes
        if (SuperOn)
        {
            var icon0 = Characters.SuperIcon(superActive);
            float isz = 56 * uiScale;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
            var rowR = new Rect(w * 0.5f - 210 * uiScale, top + 130 * uiScale, 420 * uiScale, isz);
            DrawPanel(rowR);
            GUI.DrawTexture(new Rect(rowR.x + 10 * uiScale, rowR.y, isz, isz), icon0.texture, ScaleMode.ScaleToFit);
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), rowR.center);
            OutlineLabel(new Rect(rowR.x + isz + 10 * uiScale, rowR.y, rowR.width - isz - 20 * uiScale, isz),
                Characters.SuperShortName(superActive) + "  ·  quedan " + superPipesLeft, small, new Color(1f, 0.7f, 0.25f), 2f);
            GUI.matrix = m;
        }

        // Vidas (corazones de pixel art)
        float hs = 54 * uiScale;
        var heart = PixelArt.Get("Heart", PixelArt.Heart, 20f).texture;
        for (int i = 0; i < GameConfig.MaxLives; i++)
        {
            GUI.color = i < Lives ? Color.white : new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(safe.x + 20 * uiScale + i * hs * 1.1f, top + 8 * uiScale, hs, hs), heart);
        }
        GUI.color = Color.white;

        // Poderes activos
        float py = top + 75 * uiScale;
        float px = safe.x + 20 * uiScale;
        float icon = 46 * uiScale;
        if (bird.Temp != TempPower.None && State != GameState.GameOver)
        {
            var type = bird.Temp == TempPower.Ghost ? PowerUpType.Ghost : PowerUpType.Shrink;
            GUI.DrawTexture(new Rect(px, py, icon, icon), PowerUp.Icon(type).texture);
            var barR = new Rect(px + icon + 8 * uiScale, py + icon * 0.35f, 150 * uiScale, 14 * uiScale);
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(barR, Texture2D.whiteTexture);
            GUI.color = PowerUp.TypeColor(type);
            GUI.DrawTexture(new Rect(barR.x, barR.y, barR.width * bird.TempRemaining01, barR.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            py += icon + 8 * uiScale;
        }
        if (bird.HasShield)
        {
            GUI.DrawTexture(new Rect(px, py, icon, icon), PowerUp.Icon(PowerUpType.Shield).texture);
            OutlineLabel(new Rect(px + icon + 8 * uiScale, py, 200 * uiScale, icon), "Escudo", tiny, new Color(0.6f, 0.85f, 1f), 2f, TextAnchor.MiddleLeft);
            py += icon + 8 * uiScale;
        }
        if (bird.Ammo > 0)
        {
            GUI.DrawTexture(new Rect(px, py, icon, icon), PowerUp.Icon(PowerUpType.Gun).texture);
            OutlineLabel(new Rect(px + icon + 8 * uiScale, py, 200 * uiScale, icon), "x " + bird.Ammo, small, ZoneTheme.Hex(0xffd23f), 2f, TextAnchor.MiddleLeft);
        }

        // Escenario actual
        OutlineLabel(new Rect(safe.x + 20 * uiScale, h - safe.y - 50 * uiScale, 600 * uiScale, 40 * uiScale),
            Theme.name, tiny, Theme.uiAccent, 2f, TextAnchor.MiddleLeft);

        if (State == GameState.Playing)
        {
            float s = 80 * uiScale;
            if (Button(new Rect(safe.xMax - s - 20 * uiScale, top, s, s), "II")) Pause();

            // Botón de disparo (táctil) cuando hay munición
            shootRect = new Rect(safe.xMax - 200 * uiScale, h - safe.y - 200 * uiScale, 170 * uiScale, 170 * uiScale);
            if (bird.Ammo > 0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.DrawTexture(shootRect, btnNormal);
                GUI.color = Color.white;
                var ic = new Rect(shootRect.x + shootRect.width * 0.22f, shootRect.y + shootRect.height * 0.12f, shootRect.width * 0.56f, shootRect.width * 0.56f);
                GUI.DrawTexture(ic, PowerUp.Icon(PowerUpType.Gun).texture);
                OutlineLabel(new Rect(shootRect.x, shootRect.yMax - 55 * uiScale, shootRect.width, 40 * uiScale), "DISPARAR", tiny, Color.white, 2f);
            }
        }
    }

    // Capa de colores que ondulan durante la alucinación del cigarro
    void DrawTripOverlay()
    {
        float trip = scenery.Trip;
        if (trip <= 0.01f || Event.current.type != EventType.Repaint) return;
        int stripes = 7;
        float sw = Screen.width / (float)stripes;
        for (int i = 0; i < stripes; i++)
        {
            var hue = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f + i / (float)stripes, 1f), 0.8f, 1f);
            hue.a = 0.12f * trip;
            GUI.color = hue;
            float wave = Mathf.Sin(Time.time * 2f + i) * 40f * uiScale * trip;
            GUI.DrawTexture(new Rect(i * sw + wave, 0, sw + 2, Screen.height), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void DrawBanner(Rect safe)
    {
        if (bannerTimer <= 0f || State == GameState.Menu) return;
        float k = BannerTime - bannerTimer;
        float slide = Mathf.Clamp01(Mathf.Min(k, bannerTimer) * 4f);
        float w = Mathf.Min(Screen.width - 40 * uiScale, 900 * uiScale);
        var r = new Rect((Screen.width - w) * 0.5f, safe.y + 150 * uiScale - (1f - slide) * 60 * uiScale, w, 120 * uiScale);
        GUI.color = new Color(1f, 1f, 1f, slide);
        DrawPanel(r);
        OutlineLabel(new Rect(r.x, r.y + 10 * uiScale, r.width, 55 * uiScale), bannerTitle, small, new Color(bannerColor.r, bannerColor.g, bannerColor.b, slide), 2f);
        OutlineLabel(new Rect(r.x, r.y + 60 * uiScale, r.width, 45 * uiScale), bannerSub, tiny, new Color(1f, 1f, 1f, slide), 2f);
        GUI.color = Color.white;
    }

    void DrawPopups()
    {
        foreach (var p in popups)
        {
            var sp = cam.WorldToScreenPoint(new Vector3(p.world.x, p.world.y, 0f));
            float a = 1f - Mathf.Clamp01((p.t - 0.6f) / 0.5f);
            var c = new Color(p.color.r, p.color.g, p.color.b, a);
            var r = new Rect(sp.x - 300 * uiScale, Screen.height - sp.y - 30 * uiScale, 600 * uiScale, 60 * uiScale);
            OutlineLabel(r, p.text, small, c, 2f);
        }
    }

    // ---------------------------------------------------------------- Pausa y fin

    void DrawPause()
    {
        DrawDim(0.5f);
        float h = Screen.height;
        OutlineLabel(new Rect(0, h * 0.16f, Screen.width, 100 * uiScale), "PAUSA", big, Color.white, 5f);
        float y = h * 0.36f;
        if (CenterButton(y, "CONTINUAR")) { PlaySfx(Sfx.Click); Resume(); }
        if (CenterButton(y + 100 * uiScale, "REINICIAR")) { PlaySfx(Sfx.Click); StartGame(); }
        if (CenterButton(y + 200 * uiScale, "AJUSTES")) { PlaySfx(Sfx.Click); pauseScreen = MenuScreen.Settings; confirmReset = false; }
        if (CenterButton(y + 300 * uiScale, "MENÚ PRINCIPAL")) { PlaySfx(Sfx.Click); GoToMenu(); }
    }

    void DrawGameOver()
    {
        DrawDim(0.45f);
        float w = Screen.width, h = Screen.height;
        var panel = new Rect((w - 720 * uiScale) * 0.5f, h * 0.1f, 720 * uiScale, 700 * uiScale);
        DrawPanel(panel);
        float y = panel.y + 20 * uiScale;
        OutlineLabel(new Rect(panel.x, y, panel.width, 90 * uiScale), "FIN DE LA PARTIDA", medium, ZoneTheme.Hex(0xff7070), 3f);
        y += 90 * uiScale;
        OutlineLabel(new Rect(panel.x, y, panel.width, 100 * uiScale), Score.ToString(), big, Color.white, 5f);
        y += 95 * uiScale;
        OutlineLabel(new Rect(panel.x, y, panel.width, 50 * uiScale), "Récord: " + HighScore, small, Color.white, 2f);
        y += 50 * uiScale;
        if (newRecord)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.07f;
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), new Vector2(w * 0.5f, y + 25 * uiScale));
            OutlineLabel(new Rect(panel.x, y, panel.width, 50 * uiScale), "¡NUEVO RÉCORD!", small, ZoneTheme.Hex(0xffd23f), 3f);
            GUI.matrix = m;
        }
        y += 60 * uiScale;
        OutlineLabel(new Rect(panel.x, y, panel.width, 40 * uiScale),
            "Enemigos destruidos: " + enemiesDestroyed + "   ·   Poderes: " + powerUpsTaken, tiny, new Color(0.85f, 0.9f, 1f), 2f);
        y += 40 * uiScale;
        OutlineLabel(new Rect(panel.x, y, panel.width, 40 * uiScale),
            "Zonas visitadas: " + zonesVisited, tiny, new Color(0.85f, 0.9f, 1f), 2f);
        y += 65 * uiScale;
        if (CenterButton(y, "REINTENTAR")) { PlaySfx(Sfx.Click); StartGame(); }
        if (CenterButton(y + 95 * uiScale, "MENÚ PRINCIPAL")) { PlaySfx(Sfx.Click); GoToMenu(); }
    }

    // ---------------------------------------------------------------- Utilidades de dibujo

    bool CenterButton(float y, string text, float size = 1f)
    {
        float bw = 400 * uiScale * size, bh = 80 * uiScale * size;
        return Button(new Rect((Screen.width - bw) * 0.5f, y, bw, bh), text);
    }

    bool Button(Rect r, string text)
    {
        bool clicked = GUI.Button(r, GUIContent.none, buttonStyle);
        bool pressed = r.Contains(Event.current.mousePosition) && GUIUtility.hotControl != 0;
        var tr = pressed ? new Rect(r.x, r.y + 3 * uiScale, r.width, r.height) : r;
        OutlineLabel(tr, text, buttonText, Color.white, 2.5f);
        return clicked;
    }

    GUIStyle panelStyle;

    void DrawPanel(Rect r)
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle { border = new RectOffset(4, 4, 4, 4) };
            panelStyle.normal.background = panelTex;
        }
        if (Event.current.type == EventType.Repaint) panelStyle.Draw(r, false, false, false, false);
    }

    void DrawDim(float alpha)
    {
        GUI.color = new Color(0f, 0f, 0.05f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Texto con contorno oscuro para que se lea sobre cualquier fondo.
    void OutlineLabel(Rect r, string text, GUIStyle style, Color color, float outline, TextAnchor align = TextAnchor.MiddleCenter)
    {
        if (Event.current.type != EventType.Repaint) return; // las etiquetas solo se dibujan, no reciben clics
        var prevAlign = style.alignment;
        style.alignment = align;
        float o = Mathf.Max(1f, outline * uiScale);
        style.normal.textColor = new Color(0.1f, 0.1f, 0.17f, color.a);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    GUI.Label(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), text, style);
        style.normal.textColor = new Color(0.1f, 0.1f, 0.17f, color.a * 0.6f);
        GUI.Label(new Rect(r.x, r.y + o * 2f, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
        style.alignment = prevAlign;
    }
}
