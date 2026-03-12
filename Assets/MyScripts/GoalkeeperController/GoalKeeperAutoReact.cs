using UnityEngine;
using System.Collections; // <-- Necesario para las Corrutinas

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
            animator.Update(0f); 
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
        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        int dive = Random.Range(DIR_LEFT, DIR_RIGHT + 1); 
        
        if (animator) {
            animator.SetInteger(DiveDir, dive);
            
            // Iniciamos la corrutina para resetear el valor y evitar bucles
            StartCoroutine(ResetDiveDirectionAfterTrigger());
        }
    }

    // --- MAGIA AQUÍ ---
    private IEnumerator ResetDiveDirectionAfterTrigger()
    {
        // Esperamos un cuarto de segundo. Es tiempo suficiente para que el Animator
        // detecte el número y empiece a transicionar a la animación de parada.
        yield return new WaitForSeconds(0.25f);

        if (animator) {
            // Devolvemos el parámetro a Idle (0).
            // La animación actual seguirá reproduciéndose hasta el final,
            // pero cuando termine, volverá al estado Idle en lugar de repetirse.
            animator.SetInteger(DiveDir, DIR_IDLE);
        }
    }
}