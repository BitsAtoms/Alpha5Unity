using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;          // El cuerpo completo del portero
    public Transform ball;             // La pelota real (con RealBallTracker3D)

    [Header("Movimiento del portero")]
    public float moveDistance = 0.8f;  // Distancia en eje Z (adelante/atrás)
    public float moveSpeed = 2f;       // Velocidad reducida (antes era 5)

    [Header("Detección de movimiento real de la pelota")]
    public float movementThreshold = 0.2f;
    public float quietThreshold = 0.02f;
    public float checkTime = 0.05f;

    private bool animationTriggered = false;
    private Vector3 lastBallPos;
    private float timer = 0f;
    private bool ballIsMoving = false;

    private Vector3 initialPorteroPos;

    void Start()
    {
        lastBallPos = ball.position;
        initialPorteroPos = portero.localPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= checkTime)
        {
            Vector3 currentPos = ball.position;
            float distance = Vector3.Distance(currentPos, lastBallPos);

            // Detecta movimiento rápido = chute real
            if (distance > movementThreshold)
            {
                ballIsMoving = true;

                if (!animationTriggered)
                {
                    animationTriggered = true;
                    ElegirMovimiento();
                }
            }
            else if (distance < quietThreshold)
            {
                ballIsMoving = false;
            }

            lastBallPos = currentPos;
            timer = 0f;
        }

        // Reiniciar animación si la pelota deja de moverse
        if (animationTriggered && !ballIsMoving)
        {
            animationTriggered = false;
            StartCoroutine(MoverPortero(initialPorteroPos));
        }
    }

    private void ElegirMovimiento()
    {
        int randomMove = Random.Range(0, 3);

        // 0 = Retroceder (Z negativo)
        // 1 = Quieto
        // 2 = Avanzar (Z positivo)

        if (randomMove == 0)
            StartCoroutine(MoverPortero(initialPorteroPos + Vector3.back * moveDistance));

        else if (randomMove == 1)
            StartCoroutine(MoverPortero(initialPorteroPos));

        else if (randomMove == 2)
            StartCoroutine(MoverPortero(initialPorteroPos + Vector3.forward * moveDistance));
    }

    private System.Collections.IEnumerator MoverPortero(Vector3 destino)
    {
        float t = 0;
        Vector3 inicio = portero.localPosition;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            portero.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }
    }
}
