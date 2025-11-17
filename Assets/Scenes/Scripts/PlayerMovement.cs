using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;   // qué tan rápido gira hacia la dirección de movimiento
    public bool cameraRelative = false; // si quieres que WASD sea relativo a la cámara

    Rigidbody rb;
    Vector3 inputDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D o ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S o ↑/↓
        Vector3 dir = new Vector3(h, 0f, v);
        dir = Vector3.ClampMagnitude(dir, 1f);

        if (cameraRelative && Camera.main)
        {
            // Proyecta forward/right de la cámara en el plano XZ
            Vector3 camF = Camera.main.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = Camera.main.transform.right;   camR.y = 0f; camR.Normalize();
            dir = camF * v + camR * h;
        }

        inputDir = dir;
    }

    void FixedUpdate()
    {
        // Movimiento
        Vector3 targetVel = inputDir * moveSpeed;
        Vector3 velChange = targetVel - rb.linearVelocity;
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
