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
        if (portero == null)
            portero = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (modeloHijo == null && animator != null)
            modeloHijo = animator.transform;

        baseStartLocalPos = portero.localPosition;
        baseStartLocalRot = portero.localRotation;

        if (modeloHijo != null)
        {
            childStartLocalPos = modeloHijo.localPosition;
            childStartLocalRot = modeloHijo.localRotation;
        }

        SetIdle();
    }

    // ✅ Mantenerse quieto
    public void SetIdle()
    {
        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, DIR_IDLE);
            animator.Update(0f);
        }
        Debug.Log("[GK] IDLE");
    }

    // ✅ Random entre 2 animaciones (1 y 2)

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

    // ✅ Reset SOLO al empezar nueva ronda
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
            animator.Rebind();
            animator.Update(0f);
        }

        SetIdle();
        Debug.Log("[GK] ResetForNewRound OK");
    }

    // (Opcional) si ya usas animaciones de resultado:
    public void PlayCelebrate()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 4);
        animator.Update(0f);
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 5);
        animator.Update(0f);
    }
    // 🔥 LLAMADO DESDE GAME MANAGER
    public void TriggerPerRoundAction()
    {
        // Animación random SOLO cuando se le llama
        int dive = Random.Range(1, 3); // 1 o 2
        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        if (animator != null)
        {
            animator.SetInteger("DiveDirection", dive);
            animator.Update(0f);
        }

        Debug.Log("[GK] Acción por ronda activada");
    }
}
