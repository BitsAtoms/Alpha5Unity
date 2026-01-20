using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallWASDController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 0f; // opcional (espacio), déjalo 0 si no quieres

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        // Movimiento en el plano XZ
        Vector3 dir = new Vector3(v, 0f, -h).normalized; 
        // 👆 OJO: esto depende de cómo tengas orientado tu campo.
        // Si W debe avanzar en X+, deja "v" en X.
        // Si W debe avanzar en Z+, cámbialo a new Vector3(0,0,v)

        Vector3 targetVel = dir * moveSpeed;
        Vector3 currentVel = rb.linearVelocity;

        // Mantener Y (gravedad) intacta
        rb.linearVelocity = new Vector3(targetVel.x, currentVel.y, targetVel.z);

        if (jumpForce > 0f && Input.GetKey(KeyCode.Space))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
