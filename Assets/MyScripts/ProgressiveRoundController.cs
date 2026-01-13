using UnityEngine;

public class ProgressiveRoundController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform porteroVisual;      // SOLO el modelo visual
    public GameObject dianas;             // Padre de todas las dianas

    [Header("Escalado del portero")]
    public float minKeeperScale = 1f;     // Tiro 1
    public float maxKeeperScale = 1.4f;   // Tiro 5

    Vector3 keeperBaseScale;

    void Start()
    {
        if (porteroVisual)
            keeperBaseScale = porteroVisual.localScale;

        ApplyProgressiveDifficulty();
    }

    // Este método se llamará desde el GameManager
    public void OnNewRound(int attemptIndex)
    {
        ApplyProgressiveDifficulty();
    }

    void ApplyProgressiveDifficulty()
    {
        if (GameManager.I == null)
            return;

        // attempts empieza en 0, el tiro real es +1
        int tiroActual = GameManager.I.GetCurrentAttempt() + 1;

        Debug.Log($"[PROGRESIVO] Aplicando dificultad para tiro {tiroActual}");

        // -------------------------
        // 1️⃣ DIANAS
        // -------------------------
        if (dianas)
        {
            // Aparecen a partir del tiro 3
            dianas.SetActive(tiroActual >= 3);
        }

        // -------------------------
        // 2️⃣ PORTERO MÁS GRANDE
        // -------------------------
        if (porteroVisual)
        {
            float t = Mathf.InverseLerp(1, 5, tiroActual);
            float scaleFactor = Mathf.Lerp(minKeeperScale, maxKeeperScale, t);
            porteroVisual.localScale = keeperBaseScale * scaleFactor;
        }
    }
}
