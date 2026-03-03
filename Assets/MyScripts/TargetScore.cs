using UnityEngine;

public class TargetScore : MonoBehaviour
{
    [Header("Puntos de esta diana")]
    public int points = 5;

    [Header("Anti-bug: distancia real máxima para contar como impacto")]
    public float maxHitDistance = 0.45f; // ajusta según el tamaño de tu diana

    private bool scoredThisRound = false;
    private Collider myCol;

    void Awake()
    {
        myCol = GetComponent<Collider>();
        if (myCol == null)
            Debug.LogWarning($"[TargetScore] {name} NO tiene Collider.");
        else
            myCol.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (scoredThisRound) return;
        if (!other.CompareTag("Ball")) return;

        // ✅ Evitar sumar puntos fuera del momento de tiro
        if (GameManager.I  && !GameManager.I.CanShoot())
            return;

        // ✅ Anti-bug: comprobar que la pelota está realmente cerca del centro de esta diana
        Vector3 closest = myCol  ? myCol.ClosestPoint(other.transform.position) : transform.position;
        float d = Vector3.Distance(closest, other.transform.position);

        if (d > maxHitDistance)
        {
            Debug.LogWarning($"[TargetScore] IGNORADO por distancia. Objeto={name} Dist={d:F2} > {maxHitDistance:F2}");
            return;
        }

        scoredThisRound = true;

        Debug.Log($"[TargetScore] HIT REAL -> {name} +{points}");

        if (GameManager.I)
            GameManager.I.AddTargetScore(points);
    }

    public void ResetForNewRound()
    {
        scoredThisRound = false;
    }
}
