using UnityEngine;

public class BallController : MonoBehaviour
{
    // Solo lectura desde el GameManager
    public bool TouchedKeeper { get; private set; } = false;

    // Reset al inicio de cada ronda
    public void ResetFlags()
    {
        TouchedKeeper = false;
    }

    // Si el portero usa collider normal (no trigger)
    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Goalkeeper"))
        {
            TouchedKeeper = true;
            Debug.Log("[Ball] Tocó al portero (Collision)");
        }
    }

    // Si el portero usa trigger collider
    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Goalkeeper"))
        {
            TouchedKeeper = true;
            Debug.Log("[Ball] Tocó al portero (Trigger)");
        }
    }
}
