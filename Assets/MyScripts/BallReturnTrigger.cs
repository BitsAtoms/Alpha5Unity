using UnityEngine;

public class BallReturnTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public string ballTag = "Ball";

    private bool alreadyTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered)
            return;

        if (!other.CompareTag(ballTag))
            return;

        Debug.Log("[RETURN] La pelota ha vuelto al punto de retorno");

        alreadyTriggered = true;

        // Avisamos al GameManager para reiniciar la ronda
        if (GameManager.I != null)
        {
            GameManager.I.ResetRoundExternally();
        }
    }

    public void ResetTrigger()
    {
        alreadyTriggered = false;
    }
}
