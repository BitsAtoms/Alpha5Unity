using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;          

    [Header("Modelo animado (HIJO)")]
    public Transform modeloHijo;       
    public Animator animator;

    private static readonly int DiveDir = Animator.StringToHash("DiveDirection");

    [Header("Movimiento lateral extra (opcional)")]
    public bool moverBase = true;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

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
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (modeloHijo == null && animator) modeloHijo = animator.transform;

        baseStartLocalPos = portero.localPosition;
        baseStartLocalRot = portero.localRotation;

        if (modeloHijo) {
            childStartLocalPos = modeloHijo.localPosition;
            childStartLocalRot = modeloHijo.localRotation;
        }

        SetIdle();
    }

    public void SetIdle()
    {
        if (animator) {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, DIR_IDLE);
            // Eliminado el Update(0f) para evitar tirones
        }
    }

    public void ResetForNewRound()
    {
        StopAllCoroutines();
        keeperReactedThisRound = false;

        portero.localPosition = baseStartLocalPos;
        portero.localRotation = baseStartLocalRot;

        if (modeloHijo) {
            modeloHijo.localPosition = childStartLocalPos;
            modeloHijo.localRotation = childStartLocalRot;
        }

        if (animator) {
            animator.applyRootMotion = true;
            animator.Rebind();
            animator.Update(0f); // Aquí SÍ está bien forzarlo porque es un reinicio estático
        }

        SetIdle();
    }

    public void PlayCelebrate()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 4);
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;
        keeperReactedThisRound = true;
        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, 5);
    }

    public void TriggerPerRoundAction()
    {
        int dive = Random.Range(1, 3); 
        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        if (animator) {
            animator.SetInteger(DiveDir, dive);
            // Eliminado el Update(0f) para que el salto se mezcle de forma suave
        }
    }
}