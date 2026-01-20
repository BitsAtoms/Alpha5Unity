using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameConfig", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Reglas")]
    public int maxAttempts = 5;

    [Header("Tiempos")]
    public float shotTimeout = 1f;
    public float bannerDuration = 4f;
    public float resultDisplayDuration = 3f;
    public float endGameRestartDelay = 3f;
}
