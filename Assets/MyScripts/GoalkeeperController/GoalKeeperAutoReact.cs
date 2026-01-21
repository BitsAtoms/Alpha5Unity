using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;           // base (padre/root)
    public Transform ball;

    [Header("Modelo animado (HIJO)")]
    public Transform modeloHijo;        // el objeto que tiene el Animator
    public Animator animator;

    private static readonly int DiveDir = Animator.StringToHash("DiveDirection");

    [Header("Movimiento base extra (opcional)")]
    public bool moverBase = true;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    // ===============================
    // CONTROL POR TXT
    // ===============================
    [Header("TXT allow move (solo lectura)")]
    [SerializeField] private bool externalMoveAllowed = false; // 🔥 por defecto NO se mueve

    // ===============================
    // ESTADO POR RONDA
    // ===============================
    private bool keeperReactedThisRound = false;

    // Poses iniciales (para reset)
    private Vector3 baseStartLocalPos;
    private Quaternion baseStartLocalRot;

    private Vector3 childStartLocalPos;
    private Quaternion childStartLocalRot;

    // Valores
    private const int DIR_IDLE = 0;

    // 🔥 AÑADIDO: fuerza a reaplicar el estado del TXT aunque no haya cambiado
    public void RefreshExternalState()
    {
        ApplyExternalState();
    }


    void Awake()
    {
        if (portero == null) portero = transform;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (modeloHijo == null && animator != null) modeloHijo = animator.transform;

        baseStartLocalPos = portero.localPosition;
        baseStartLocalRot = portero.localRotation;

        if (modeloHijo != null)
        {
            childStartLocalPos = modeloHijo.localPosition;
            childStartLocalRot = modeloHijo.localRotation;
        }

        // 🔥 CLAVE: el Animator empieza APAGADO para que jamás se “cuele” una animación
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.enabled = false;
        }
    }

    // =====================================================
    // 🔒 APLICAR ESTADO SEGÚN TXT
    // =====================================================
    public void SetExternalMoveAllowed(bool allow)
    {
        externalMoveAllowed = allow;
        Debug.Log("[GK] externalMoveAllowed=" + externalMoveAllowed);

        ApplyExternalState();
    }

    private void ApplyExternalState()
    {
        StopAllCoroutines();

        // Siempre reseteamos la pose base y el hijo
        portero.localPosition = baseStartLocalPos;
        portero.localRotation = baseStartLocalRot;

        if (modeloHijo != null)
        {
            modeloHijo.localPosition = childStartLocalPos;
            modeloHijo.localRotation = childStartLocalRot;
        }

        if (animator == null) return;

        // 🔥 Forzamos el parámetro a Idle SIEMPRE antes de decidir
        // (aunque esté disabled, dejamos el parámetro “limpio”)
        animator.enabled = true;                 // lo encendemos 1 frame para poder setear seguro
        animator.applyRootMotion = false;
        animator.Rebind();
        animator.Update(0f);
        animator.SetInteger(DiveDir, DIR_IDLE);
        animator.Update(0f);

        if (!externalMoveAllowed)
        {
            // TXT=0 -> NO ANIMACIÓN, NO ROOT, Animator OFF
            animator.applyRootMotion = false;
            animator.enabled = false;

            Debug.Log("[GK] TXT=0 -> Animator OFF, portero QUIETO");
        }
        else
        {
            // TXT=1 -> permitir animación (root motion activo)
            animator.applyRootMotion = true;
            animator.enabled = true;

            Debug.Log("[GK] TXT=1 -> Animator ON, puede moverse");
        }
    }

    // =====================================================
    // 🎯 LLAMADO AL INICIO DE CADA RONDA
    // =====================================================
    public void TriggerPerRoundAction()
    {
        // Si TXT=0 -> ni siquiera intentamos nada
        if (!externalMoveAllowed)
        {
            Debug.Log("[GK] TriggerPerRoundAction: TXT=0 -> NO hace nada");
            keeperReactedThisRound = true; // “consume” la acción de ronda
            return;
        }

        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        DoRandomDive();
    }

    private void DoRandomDive()
    {
        if (animator == null) return;

        // 1..3 incluidos
        int diveValue = Random.Range(1, 4);

        animator.enabled = true;
        animator.applyRootMotion = true;

        animator.SetInteger(DiveDir, diveValue);
        animator.Update(0f);

        Debug.Log("[GK] DiveDirection=" + diveValue);

        // movimiento extra en base para 1 y 2
        if (moverBase && (diveValue == 1 || diveValue == 2))
        {
            StopAllCoroutines();
            StartCoroutine(MoverBaseExtra(diveValue));
        }
    }

    private System.Collections.IEnumerator MoverBaseExtra(int diveValue)
    {
        Vector3 destino = baseStartLocalPos;

        // OJO: tus ejes originales:
        if (diveValue == 1) destino += Vector3.back * moveDistance;
        else if (diveValue == 2) destino += Vector3.forward * moveDistance;

        Vector3 inicio = portero.localPosition;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            portero.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }

        portero.localPosition = destino;
    }

    // =====================================================
    // 🔄 Reset SOLO cuando empieza nueva ronda
    // =====================================================
    public void ResetForNewRound()
    {
        StopAllCoroutines();
        keeperReactedThisRound = false;

        // reset poses
        portero.localPosition = baseStartLocalPos;
        portero.localRotation = baseStartLocalRot;

        if (modeloHijo != null)
        {
            modeloHijo.localPosition = childStartLocalPos;
            modeloHijo.localRotation = childStartLocalRot;
        }

        // reaplicar TXT state (para que si está en 0 se quede apagado)
        ApplyExternalState();

        Debug.Log("[GK] ResetForNewRound OK");
    }

    // =====================================================
    // 🎉 RESULTADOS (si quieres que SIEMPRE pueda animar aquí)
    // =====================================================
    public void PlayCelebrate()
    {
        if (animator == null) return;

        animator.enabled = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 4);
        animator.Update(0f);
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;

        animator.enabled = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 5);
        animator.Update(0f);
    }
}
