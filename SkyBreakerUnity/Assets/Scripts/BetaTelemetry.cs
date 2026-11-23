using System.Globalization;
using System.IO;
using UnityEngine;

// Registro de las sesiones de prueba de la versión Beta.
// Cada partida agrega una línea a un archivo CSV con el tester, el personaje, los puntos,
// la duración, la causa de la derrota, los FPS y los errores detectados. El archivo sirve
// como evidencia para el informe de pruebas y se abre con Excel.
public static class BetaTelemetry
{
    public const string Version = "0.9.0-beta";
    const string TesterKey = "SkyBreaker_Tester";
    const string ShowFpsKey = "SkyBreaker_ShowFps";

    static bool hooked;
    static float runStart;
    static int errors;
    static int totalErrors;
    static double fpsSum;
    static int fpsFrames;
    static float minFps;
    static float smoothFps = 60f;

    public static string FilePath => Path.Combine(Application.persistentDataPath, "SkyBreaker_pruebas_beta.csv");
    public static string ErrorLogPath => Path.Combine(Application.persistentDataPath, "SkyBreaker_errores_beta.txt");
    public static float CurrentFps => smoothFps;
    public static int SessionErrors => totalErrors;

    public static int TesterId
    {
        get => PlayerPrefs.GetInt(TesterKey, 1);
        set => PlayerPrefs.SetInt(TesterKey, Mathf.Clamp(value, 1, 20));
    }

    public static bool ShowFps
    {
        get => PlayerPrefs.GetInt(ShowFpsKey, 0) == 1;
        set => PlayerPrefs.SetInt(ShowFpsKey, value ? 1 : 0);
    }

    public static void Init()
    {
        if (hooked) return;
        hooked = true;
        Application.logMessageReceived += OnLog;
    }

    // Cuenta los errores y excepciones que ocurran durante la sesión y los guarda en un archivo
    static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        errors++;
        totalErrors++;
        try
        {
            File.AppendAllText(ErrorLogPath, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                " | tester " + TesterId + " | " + message + "\n" + stack + "\n");
        }
        catch { }
    }

    public static void StartRun()
    {
        runStart = Time.time;
        errors = 0;
        fpsSum = 0;
        fpsFrames = 0;
        minFps = float.MaxValue;
    }

    // Se llama cada cuadro con el tiempo real transcurrido
    public static void Tick(float unscaledDt, bool playing)
    {
        if (unscaledDt <= 0f) return;
        float fps = 1f / unscaledDt;
        smoothFps = Mathf.Lerp(smoothFps, fps, 0.1f);
        if (!playing) return;
        fpsSum += fps;
        fpsFrames++;
        if (fpsFrames > 30 && fps < minFps) minFps = fps; // ignora los primeros cuadros de carga
    }

    public static void EndRun(string character, string startZone, int score, string cause,
        int zonesVisited, int powerUps, int enemiesDestroyed)
    {
        var inv = CultureInfo.InvariantCulture;
        float duration = Time.time - runStart;
        float avgFps = fpsFrames > 0 ? (float)(fpsSum / fpsFrames) : 0f;
        float min = minFps == float.MaxValue ? avgFps : minFps;
        string line = string.Join(",",
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TesterId.ToString(inv),
            Version,
            Csv(character),
            Csv(startZone),
            score.ToString(inv),
            duration.ToString("0.0", inv),
            Csv(cause),
            zonesVisited.ToString(inv),
            powerUps.ToString(inv),
            enemiesDestroyed.ToString(inv),
            avgFps.ToString("0", inv),
            min.ToString("0", inv),
            errors.ToString(inv),
            Csv(Application.platform.ToString()),
            Screen.width + "x" + Screen.height);
        try
        {
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "fecha,tester,version,personaje,escenario_inicial,puntos,duracion_s,causa,zonas,poderes,enemigos_destruidos,fps_promedio,fps_minimo,errores,plataforma,resolucion\n");
            File.AppendAllText(FilePath, line + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo guardar el registro de pruebas: " + e.Message);
        }
    }

    static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "'") + "\"";
}
