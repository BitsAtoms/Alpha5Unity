using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;          // base (padre/root)

    [Header("Modelo animado (HIJO)")]
    public Transform modeloHijo;       // hijo con Animator
    public Animator animator;

    private static readonly int DiveDir = Animator.StringToHash("DiveDirection");

    [Header("Movimiento lateral extra (opcional)")]
    public bool moverBase = true;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    // ✅ SOLO 1 ACCIÓN POR RONDA
    private bool keeperReactedThisRound = false;

    private const int DIR_IDLE  = 0;
    private const int DIR_LEFT  = 1;
    private const int DIR_RIGHT = 2;

    private Vector3 baseStartLocalPos;
    private Quaternion baseStartLocalRot;

    private Vector3 childStartLocalPos;
    private Quaternion childStartLocalRot;

    void Awake()
    {
        if (portero == null) portero = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (modeloHijo == null && animator != null) modeloHijo = animator.transform;

        baseStartLocalPos = portero.localPosition;
        baseStartLocalRot = portero.localRotation;

        if (modeloHijo != null)
        {
            childStartLocalPos = modeloHijo.localPosition;
            childStartLocalRot = modeloHijo.localRotation;
        }
    }

    void Start()
    {
        // Importante: solo un reset inicial limpio
        ResetForNewRound();
    }

    public void SetIdle()
    {
        if (animator != null)
        {
            // Deja root motion como lo usabas antes (true)
            animator.applyRootMotion = true;

            // NO Rebind aquí (rompe las animaciones)
            animator.SetInteger(DiveDir, DIR_IDLE);
        }

        Debug.Log("[GK] IDLE");
    }

    private System.Collections.IEnumerator MoverBaseExtra(int diveValue)
    {
        Vector3 destino = baseStartLocalPos;

        if (diveValue == DIR_LEFT)
            destino += Vector3.back * moveDistance;
        else if (diveValue == DIR_RIGHT)
            destino += Vector3.forward * moveDistance;

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

    public void ResetForNewRound()
    {
        StopAllCoroutines();
        keeperReactedThisRound = false;

        portero.localPosition = baseStartLocalPos;
        portero.localRotation = baseStartLocalRot;

        if (modeloHijo != null)
        {
            modeloHijo.localPosition = childStartLocalPos;
            modeloHijo.localRotation = childStartLocalRot;
        }

        if (animator != null)
        {
            animator.applyRootMotion = true;

            // ✅ Rebind SOLO aquí (reset de ronda)
            animator.Rebind();
            animator.Update(0f);
        }

        SetIdle();
        Debug.Log("[GK] ResetForNewRound OK");
    }

    public void PlayCelebrate()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 4);
        Debug.Log("[GK] Celebrate");
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 5);
        Debug.Log("[GK] Disappointed");
    }

    public void TriggerPerRoundAction()
    {
        if (animator == null)
        {
            Debug.LogWarning("[GK] TriggerPerRoundAction() pero animator es NULL");
            return;
        }

        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        int diveValue = Random.Range(1, 4); // 1..3
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, diveValue);

        Debug.Log($"✅ [GK] TriggerPerRoundAction() -> DiveDirection={diveValue}");

        if (moverBase && (diveValue == DIR_LEFT || diveValue == DIR_RIGHT))
        {
            StopAllCoroutines();
            StartCoroutine(MoverBaseExtra(diveValue));
        }
    }
}
