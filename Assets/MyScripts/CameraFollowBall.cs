using UnityEngine;

public class CameraFollowBall : MonoBehaviour
{
    public Transform ball;        // referencia a la pelota (el objeto que mueve RealBallTracker3D)
    public Vector3 offset = new Vector3(0f, 3f, -6f); // distancia fija detrás
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (ball == null)
            return;

        // posición deseada = pelota + offset
        Vector3 desiredPosition = ball.position + offset;

        // movimiento suave
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // mirar siempre a la pelota
        transform.LookAt(ball);
    }
}
