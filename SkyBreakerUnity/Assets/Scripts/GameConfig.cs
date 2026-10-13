using UnityEngine;

// Valores de ajuste del juego. Se modifican aquí después de las pruebas de jugabilidad.
public static class GameConfig
{
    // Vuelo de Pico
    public const float Gravity = -26f;
    public const float FlapVelocity = 8f;
    public const float MaxFallSpeed = -14f;
    public const float BirdRadius = 0.32f;

    // Escenario
    public const float GroundY = -4.3f;
    public const float CeilingY = 4.6f;
    public const float StartSpeed = 3f;
    public const float MaxSpeed = 5.5f;

    // Tuberías
    public const float PipeWidth = 1.2f;
    public const float PipeSpacing = 4.2f;
    public const float StartGap = 3.4f;
    public const float MinGap = 2.3f;
    public const float MaxGapCenterJump = 2.6f;

    // Dificultad: a partir de esta puntuación empieza la fase de maestría
    public const int MasteryScore = 100;

    // Vidas
    public const int StartLives = 1;
    public const int MaxLives = 3;
    public const float InvulnerableTime = 2f;

    // Poderes
    public const float ShrinkDuration = 8f;
    public const int FirstPowerUpPipes = 5;
    public const int PowerUpMinPipes = 8;
    public const int PowerUpMaxPipes = 12;
    public const float PowerUpRadius = 0.4f;

    // Enemigos (murciélago mecánico)
    public const int EnemyStartScore = 20;
    public const float EnemyChanceStart = 0.3f;
    public const float EnemyChanceMax = 0.7f;
    public const float EnemyExtraSpeed = 2.5f;
    public const float EnemyRadius = 0.32f;
    public const int EnemyKillPoints = 2;

    public const string HighScoreKey = "SkyBreaker_HighScore";

    public static bool CircleOverlapsRect(Vector2 c, float r, float xMin, float xMax, float yMin, float yMax)
    {
        float nx = Mathf.Clamp(c.x, xMin, xMax);
        float ny = Mathf.Clamp(c.y, yMin, yMax);
        float dx = c.x - nx;
        float dy = c.y - ny;
        return dx * dx + dy * dy < r * r;
    }
}
