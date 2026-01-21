using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;
    public Transform ball;

    [Header("Modelo animado (HIJO)")]
    public Transform modeloHijo;
    public Animator animator;

    private static readonly int DiveDir = Animator.StringToHash("DiveDirection");

    [Header("Movimiento lateral extra (opcional)")]
    public bool moverBase = true;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    // ✅ Importante: NO reacciona a la pelota
    [Header("Reacción por pelota (NO USAR)")]
    public bool reactToBallMovement = false;

    private Vector3 baseStartLocalPos;
    private Quaternion baseStartLocalRot;

    private Vector3 childStartLocalPos;
    private Quaternion childStartLocalRot;

    private bool keeperReactedThisRound = false;

    private const int DIR_IDLE = 0;
    private const int DIR_LEFT = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_JUMP = 3;
    private const int DIR_CELEBRATE = 4;
    private const int DIR_DISAPPOINTED = 5;

    [Header("Control externo (TXT)")]
    public bool externalMoveAllowed = true;

    private bool AllowMove() => externalMoveAllowed;

    public void SetExternalMoveAllowed(bool allow)
    {
        externalMoveAllowed = allow;
        Debug.Log("[GK] externalMoveAllowed=" + externalMoveAllowed);

        if (!externalMoveAllowed)
        {
            StopAllCoroutines();
            if (animator != null)
            {
                animator.applyRootMotion = true;
                animator.SetInteger(DiveDir, DIR_IDLE);
                animator.Update(0f);
            }
        }
    }

    void Start()
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

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, DIR_IDLE);
            animator.Update(0f);
        }

        Debug.Log("[GK] Start OK. Pulsa P para probar animación manual.");
    }

    void Update()
    {
        // ✅ TEST: si con P se mueve, el GK+Animator están bien
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[GK] TEST P -> TriggerRandomDiveThisRound_NoShotWindow()");
            TriggerRandomDiveThisRound_NoShotWindow();
        }

        // ✅ No usamos la pelota para disparar nada
        if (!reactToBallMovement) return;
    }

    private void ElegirMovimiento()
    {
        if (!AllowMove())
        {
            Debug.Log("[GK] ElegirMovimiento BLOQUEADO (TXT=0)");
            return;
        }

        int diveValue = Random.Range(1, 4);

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, diveValue);
            animator.Update(0f);
            Debug.Log("[GK] DiveDirection=" + diveValue);
        }

        if (moverBase && (diveValue == DIR_LEFT || diveValue == DIR_RIGHT))
        {
            StopAllCoroutines();
            StartCoroutine(MoverBaseExtra(diveValue));
        }
    }

    private System.Collections.IEnumerator MoverBaseExtra(int diveValue)
    {
        if (!AllowMove()) yield break;

        Vector3 destino = baseStartLocalPos;

        if (diveValue == DIR_LEFT) destino += Vector3.back * moveDistance;
        else if (diveValue == DIR_RIGHT) destino += Vector3.forward * moveDistance;

        Vector3 inicio = portero.localPosition;

        float t = 0f;
        while (t < 1f)
        {
            if (!AllowMove()) yield break;
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
            animator.Rebind();
            animator.Update(0f);
            animator.SetInteger(DiveDir, DIR_IDLE);
            animator.Update(0f);
        }

        Debug.Log("[GK] ResetForNewRound OK");
    }

    // ✅ Este es el que vamos a llamar desde el TXT
    public void TriggerRandomDiveThisRound_NoShotWindow()
    {
        if (!AllowMove())
        {
            Debug.Log("[GK] TriggerRandomDive IGNORADO (TXT=0)");
            return;
        }

        if (keeperReactedThisRound)
        {
            Debug.Log("[GK] TriggerRandomDive IGNORADO (ya reaccionó esta ronda)");
            return;
        }

        keeperReactedThisRound = true;
        ElegirMovimiento();
        Debug.Log("[GK] TriggerRandomDive OK - Animación lanzada");
    }

    // ====================================================
    // 🔥 ANIMACIONES POR RESULTADO (NO dependen del TXT)
    // ====================================================
    public void PlayCelebrate()
    {
        if (animator == null) return;

        StopAllCoroutines();

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_CELEBRATE);
        animator.Update(0f);

        Debug.Log("[GK] PlayCelebrate()");
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;

        StopAllCoroutines();

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_DISAPPOINTED);
        animator.Update(0f);

        Debug.Log("[GK] PlayDisappointed()");
    }

    public void RearmFromExternalTrigger()
    {
        // Solo rearma el candado, sin mover posiciones
        keeperReactedThisRound = false;
        Debug.Log("[GK] RearmFromExternalTrigger() -> listo para nueva animación por TXT");
    }
}