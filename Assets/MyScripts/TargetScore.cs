using UnityEngine;

public class TargetScore : MonoBehaviour
{
    public int points = 25;
    bool alreadyHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyHit) return;
        if (!other.CompareTag("Ball")) return;

        alreadyHit = true;

        if (GameManager.I != null)
            GameManager.I.AddTargetScore(points);

        Debug.Log("[DIANA] Impacto detectado + " + points + " puntos");
    }

    public void ResetTarget()
    {
        alreadyHit = false;
    }
}
