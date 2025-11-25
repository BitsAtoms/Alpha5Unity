using UnityEngine;

public class CameraFollowBall : MonoBehaviour
{
    [Header("Pelota a seguir")]
    public Transform ball;

    [Header("Ajustes de la cámara")]
    public float distance = 8f;     // Distancia detrás de la pelota
    public float height = 3f;       // Altura de la cámara
    public float rotationSmooth = 5f;  // Suavizado de rotación
    public float positionSmooth = 5f;  // Suavizado de movimiento

    private Vector3 lastForward = Vector3.forward; // última dirección estable usada

    private void LateUpdate()
    {
        if (!ball) return;

        // Se calcula la dirección hacia la que se mueve la pelota
        Vector3 ballVelocity = Vector3.zero;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
            ballVelocity = rb.linearVelocity;

        Vector3 forwardDir;

        // Si la pelota se está moviendo, usamos su dirección real
        if (ballVelocity.magnitude > 0.1f)
        {
            forwardDir = ballVelocity.normalized;
            lastForward = forwardDir; // guardar dirección estable
        }
        else
        {
            // Si está quieta, usa la última dirección válida
            forwardDir = lastForward;
        }

        // Posición objetivo detrás de la pelota
        Vector3 targetPos = ball.position - forwardDir * distance + Vector3.up * height;

        // Movimiento suave
        transform.position = Vector3.Lerp(transform.position, targetPos, positionSmooth * Time.deltaTime);

        // La cámara siempre mira la pelota
        Vector3 lookPoint = ball.position + Vector3.up * 1f;
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
    }
}
