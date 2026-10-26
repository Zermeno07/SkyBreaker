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
    public const int CrackedPipesScore = 25;   // tuberías agrietadas (se rompen con un disparo)
    public const int GhostPipesScore = 30;     // tuberías fantasma (se pueden traspasar)
    public const float SpecialPipeChanceMax = 0.3f;

    // Dificultad: a partir de esta puntuación empieza la fase de maestría
    public const int MasteryScore = 100;

    // Escenarios: cada cuántos puntos se cambia de escenario durante la partida
    public const int PointsPerZone = 40;

    // Vidas
    public const int StartLives = 1;
    public const int MaxLives = 3;
    public const float InvulnerableTime = 2f;

    // Poderes
    public const float ShrinkDuration = 8f;
    public const float GhostDuration = 5f;
    public const int GunShots = 5;
    public const int MaxAmmo = 10;
    public const float ShotSpeed = 13f;
    public const float ShotCooldown = 0.22f;
    public const int FirstPowerUpPipes = 4;
    public const int PowerUpMinPipes = 6;
    public const int PowerUpMaxPipes = 9;
    public const float PowerUpRadius = 0.45f;
    // Puntuación a partir de la cual aparece cada poder
    public const int GunUnlockScore = 8;
    public const int GhostUnlockScore = 12;
    public const int ShieldUnlockScore = 18;

    // Enemigos: puntuación a la que aparece cada uno por primera vez.
    // Murciélago 10, avispa 15, dron 20, cuervo 25.
    public const int FirstEnemyScore = 10;
    public const int EnemyUnlockStep = 5;
    // Probabilidad de que aparezca un enemigo con cada tubería: empieza baja y sube poco a poco.
    public const float EnemyChanceStart = 0.18f;
    public const float EnemyChancePerPoint = 0.005f;
    public const float EnemyChanceMax = 0.5f;
    // Al ataque de los enemigos se le da más tiempo de espera al principio
    public const float EnemyAttackSlowStart = 1.5f;

    public const string HighScoreKey = "SkyBreaker_HighScore";
    public const string MusicVolumeKey = "SkyBreaker_Music";
    public const string SfxVolumeKey = "SkyBreaker_Sfx";
    public const string VibrationKey = "SkyBreaker_Vibration";
    public const string StartZoneKey = "SkyBreaker_StartZone";

    public static bool CircleOverlapsRect(Vector2 c, float r, float xMin, float xMax, float yMin, float yMax)
    {
        float nx = Mathf.Clamp(c.x, xMin, xMax);
        float ny = Mathf.Clamp(c.y, yMin, yMax);
        float dx = c.x - nx;
        float dy = c.y - ny;
        return dx * dx + dy * dy < r * r;
    }
}
