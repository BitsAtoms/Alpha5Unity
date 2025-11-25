using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class KeeperPingPong : MonoBehaviour
{
    [Header("Movimiento")]
    public float amplitude = 3f;   // Distancia máxima desde el punto inicial (mitad del recorrido total)
    public float speed = 2f;       // Velocidad del movimiento (unidades por segundo)
    public float centerOffset = 0f;// Desplazamiento del centro desde la posición inicial

    [Header("Comportamiento")]
    public bool pauseAtEnds = false;
    public float endPauseTime = 0.2f;
    public bool drawGizmos = true;

    private Rigidbody rb;
    private Vector3 startPos;
    private float pauseTimer = 0f;
    private int dir = 1; // 1 = hacia adelante, -1 = hacia atrás
    private float traveled = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        if (Application.isPlaying)
            startPos = transform.position;
        else if (startPos == Vector3.zero)
            startPos = transform.position;
    }

    void OnEnable()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Start()
    {
        startPos = transform.position;
        traveled = 0f;
        dir = 1;
        pauseTimer = 0f;
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            // En modo edición, coloca en el centro de la trayectoria
            Vector3 p = startPos;
            p.z = startPos.z + centerOffset;
            transform.position = p;
            return;
        }

        if (pauseAtEnds && pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        float step = speed * Time.fixedDeltaTime * dir;
        traveled += step;

        // Rebote en extremos [-amplitude, +amplitude]
        float extent = amplitude;
        if (traveled > extent)
        {
            traveled = extent;
            dir = -1;
            if (pauseAtEnds) pauseTimer = endPauseTime;
        }
        else if (traveled < -extent)
        {
            traveled = -extent;
            dir = 1;
            if (pauseAtEnds) pauseTimer = endPauseTime;
        }

        // Movimiento sobre el eje Z
        Vector3 target = startPos;
        target.z = startPos.z + centerOffset + traveled;

        rb.MovePosition(target);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Vector3 a = startPos; 
        Vector3 b = startPos;
        a.z += centerOffset - amplitude;
        b.z += centerOffset + amplitude;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.1f);
        Gizmos.DrawSphere(b, 0.1f);
    }
}
