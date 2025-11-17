using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;   // qué tan rápido gira hacia la dirección de movimiento

    private Rigidbody rb;
    private Vector3 inputDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // Lectura de input
        float h = Input.GetAxisRaw("Horizontal"); // A/D o ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S o ↑/↓

        // Dirección en función de la cámara (para que W sea "hacia donde miro")
        Vector3 dir = Vector3.zero;

        if (Camera.main != null)
        {
            // Forward y Right de la cámara proyectados al plano XZ
            Vector3 camF = Camera.main.transform.forward;
            camF.y = 0f;
            camF.Normalize();

            Vector3 camR = Camera.main.transform.right;
            camR.y = 0f;
            camR.Normalize();

            dir = camF * v + camR * h;
        }
        else
        {
            // Por si no hay cámara, usamos ejes del mundo
            dir = new Vector3(h, 0f, v);
        }

        // Limitar magnitud a 1 para que en diagonal no vaya más rápido
        dir = Vector3.ClampMagnitude(dir, 1f);

        inputDir = dir;
    }

    void FixedUpdate()
    {
        // Movimiento
        Vector3 targetVel = inputDir * moveSpeed;
        Vector3 velChange = targetVel - rb.linearVelocity; // <- corregido
        velChange.y = 0f;
        rb.AddForce(velChange, ForceMode.VelocityChange);

        // Rotación hacia la dirección de avance (si hay input)
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
