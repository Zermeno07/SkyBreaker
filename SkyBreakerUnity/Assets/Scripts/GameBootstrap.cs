using UnityEngine;

// Arranca el juego automáticamente al presionar Play en cualquier escena,
// sin necesidad de configurar objetos a mano en el editor.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }
}
