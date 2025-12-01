using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;   // hijo local
    public Transform ball;

    [Header("Movimiento del portero")]
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    [Header("Detección del movimiento real de la pelota")]
    public float movementThreshold = 0.25f;   // SUBIDO: detección más realista
    public float quietThreshold = 0.02f;
    public float checkTime = 0.05f;

    private bool animationTriggered = false;
    private Vector3 lastBallPos;
    private float timer = 0f;

    private Vector3 localStart;
    private float savedLocalY;

    void Start()
    {
        lastBallPos = ball.position;

        localStart = portero.localPosition;
        savedLocalY = localStart.y;

        Debug.Log("[GK] Iniciado correctamente. Posición local = " + localStart);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= checkTime)
        {
            float distance = Vector3.Distance(ball.position, lastBallPos);

            // --- Detectar CHUTÉ REAL ---
            if (distance > movementThreshold)
            {
                Debug.Log("[GK] CHUTE DETECTADO. Distancia = " + distance);

                if (!animationTriggered)
                {
                    animationTriggered = true;

                    // ACTIVAR ventana de tiro REAL
                    if (GameManager.I != null)
                    {
                        GameManager.I.ArmShotWindow();
                        Debug.Log("[GK] Ventana de tiro ARMADA desde portero");
                    }

                    ElegirMovimiento();
                }
            }

            lastBallPos = ball.position;
            timer = 0f;
        }

        // aseguramos altura fija
        Vector3 p = portero.localPosition;
        p.y = savedLocalY;
        portero.localPosition = p;
    }

    public void ResetForNewRound()
    {
        animationTriggered = false;
        StopAllCoroutines();

        portero.localPosition = localStart;

        Vector3 p = portero.localPosition;
        p.y = savedLocalY;
        portero.localPosition = p;

        lastBallPos = ball.position;

        Debug.Log("[GK] Reset de ronda completado");
    }

    private void ElegirMovimiento()
    {
        int r = Random.Range(0, 3);
        Vector3 destino = localStart;

        if (r == 0) destino += Vector3.back * moveDistance;
        else if (r == 2) destino += Vector3.forward * moveDistance;

        StartCoroutine(MoverPortero(destino));
    }

    private System.Collections.IEnumerator MoverPortero(Vector3 destino)
    {
        destino.y = savedLocalY;

        Vector3 inicio = portero.localPosition;
        inicio.y = savedLocalY;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            portero.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }

        Vector3 p = portero.localPosition;
        p.y = savedLocalY;
        portero.localPosition = p;
    }
}
