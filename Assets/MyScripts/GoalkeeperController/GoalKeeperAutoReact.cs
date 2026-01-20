using UnityEngine;

public class GoalkeeperAutoReact : MonoBehaviour
{
    [Header("Referencias")]
    public Transform portero;          // base (padre/root)
    public Transform ball;

    [Header("Modelo animado (HIJO)")]
    public Transform modeloHijo;       // este debe poder moverse libremente por animación
    public Animator animator;

    private static readonly int DiveDir = Animator.StringToHash("DiveDirection");

    [Header("Movimiento lateral extra (opcional)")]
    public bool moverBase = true;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    [Header("Detección del tiro")]
    public float movementThreshold = 0.25f;
    public float checkTime = 0.05f;

    private Vector3 lastBallPos;
    private float timer = 0f;

    // Guardamos poses iniciales (para reset SOLO en nueva ronda)
    private Vector3 baseStartLocalPos;
    private Quaternion baseStartLocalRot;

    private Vector3 childStartLocalPos;
    private Quaternion childStartLocalRot;

    // ====================================================
    // ✅ SOLO 1 ANIMACIÓN POR RONDA
    // ====================================================
    private bool shotDetectedThisRound = false;
    private bool keeperReactedThisRound = false;

    // ====================================================
    // 🔥 Valores para animaciones
    // ====================================================
    private const int DIR_IDLE = 0;
    private const int DIR_LEFT = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_JUMP = 3;
    private const int DIR_CELEBRATE = 4;     // jugador falla
    private const int DIR_DISAPPOINTED = 5;  // jugador marca gol

    // ====================================================
    // 🔥🔥🔥 CONTROL EXTERNO POR TXT (0/1)
    // ====================================================
    [Header("Control externo (TXT)")]
    public bool externalMoveAllowed = true;  // lo cambia KeeperMoveFlagReader

    // ✅ AÑADIDO: candado central
    private bool AllowMove()
    {
        if (!externalMoveAllowed)
        {
            // Debug mínimo para no spamear, pero útil si algo intenta moverlo
            // (si quieres más, puedes activar logs aquí)
            return false;
        }
        return true;
    }

    public void SetExternalMoveAllowed(bool allow)
    {
        externalMoveAllowed = allow;
        Debug.Log("[GK] externalMoveAllowed=" + externalMoveAllowed);

        // ✅ SI EL TXT PASA A 0: paramos TODO movimiento/animación de dive y volvemos a Idle
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

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, DIR_IDLE);
            animator.Update(0f);
        }

        lastBallPos = ball != null ? ball.position : Vector3.zero;
    }

    void Update()
    {
        if (ball == null) return;

        // ✅ CANDADO: si TXT=0, jamás reaccionar por movimiento de pelota
        if (!AllowMove()) return;

        // ✅ si ya reaccionó esta ronda, no hacemos nada más
        if (keeperReactedThisRound) return;

        timer += Time.deltaTime;

        if (timer >= checkTime)
        {
            float distance = Vector3.Distance(ball.position, lastBallPos);

            if (distance > movementThreshold)
            {
                float deltaX = ball.position.x - lastBallPos.x;

                // ✅ Detectar chute una sola vez por ronda
                if (deltaX > 0f && !shotDetectedThisRound)
                {
                    shotDetectedThisRound = true;

                    if (GameManager.I != null)
                        GameManager.I.ArmShotWindow();

                    TriggerKeeperReactionOnce();
                }
            }

            lastBallPos = ball.position;
            timer = 0f;
        }
    }

    private void TriggerKeeperReactionOnce()
    {
        // ✅ CANDADO extra por seguridad
        if (!AllowMove()) return;

        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        ElegirMovimiento();
    }

    private void ElegirMovimiento()
    {
        // ✅ CANDADO extra por seguridad
        if (!AllowMove()) return;

        // ✅ 1,2,3 (3 incluido)
        int diveValue = Random.Range(1, 4);

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, diveValue);
            animator.Update(0f);
            Debug.Log("[GK] DiveDirection=" + diveValue);
        }

        // ✅ Movimiento extra solo para 1 y 2 (izquierda/derecha)
        if (moverBase && (diveValue == DIR_LEFT || diveValue == DIR_RIGHT))
        {
            StopAllCoroutines();
            StartCoroutine(MoverBaseExtra(diveValue));
        }
    }

    private System.Collections.IEnumerator MoverBaseExtra(int diveValue)
    {
        // ✅ CANDADO extra por seguridad
        if (!AllowMove()) yield break;

        Vector3 destino = baseStartLocalPos;

        if (diveValue == DIR_LEFT)
            destino += Vector3.back * moveDistance;
        else if (diveValue == DIR_RIGHT)
            destino += Vector3.forward * moveDistance;

        Vector3 inicio = portero.localPosition;

        float t = 0f;
        while (t < 1f)
        {
            // ✅ si en mitad del movimiento el TXT pasa a 0, cortamos
            if (!AllowMove()) yield break;

            t += Time.deltaTime * moveSpeed;
            portero.localPosition = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }

        portero.localPosition = destino;
    }

    // ✅ Reset SOLO cuando empieza nueva ronda
    public void ResetForNewRound()
    {
        StopAllCoroutines();

        shotDetectedThisRound = false;
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

        lastBallPos = ball != null ? ball.position : Vector3.zero;

        Debug.Log("[GK] ResetForNewRound OK → listo para 1 animación en esta ronda");
    }

    // Para activación externa (cámara/LiDAR)
    public void OnShotDetected(float dummySpeed)
    {
        // ✅ TXT manda: si 0, no se mueve
        if (!AllowMove()) return;

        if (keeperReactedThisRound) return;

        shotDetectedThisRound = true;

        if (GameManager.I != null)
            GameManager.I.ArmShotWindow();

        TriggerKeeperReactionOnce();
    }

    // ====================================================
    // ✅ AL INICIO DE CADA RONDA, 1 ANIMACIÓN RANDOM + TIMEOUT
    // ====================================================
    public void TriggerRandomDiveThisRound_NoShotWindow()
    {
        // ✅ TXT manda: si 0, no se mueve
        if (!AllowMove())
        {
            Debug.Log("[GK] TriggerRandomDiveThisRound_NoShotWindow IGNORADO (TXT=0)");
            return;
        }

        if (keeperReactedThisRound)
        {
            Debug.Log("[GK] TriggerRandomDiveThisRound_NoShotWindow IGNORADO (ya reaccionó esta ronda)");
            return;
        }

        keeperReactedThisRound = true;

        // ✅ arrancar timeout de fallo (si quieres)
        if (GameManager.I != null)
            GameManager.I.ArmShotWindow();

        ElegirMovimiento();
        Debug.Log("[GK] TriggerRandomDiveThisRound_NoShotWindow OK");
    }

    // ====================================================
    // 🔥 ANIMACIONES POR RESULTADO (NO dependen del TXT)
    // ====================================================
    public void PlayCelebrate()
    {
        if (animator == null) return;

        keeperReactedThisRound = true;
        shotDetectedThisRound = true;

        StopAllCoroutines();

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_CELEBRATE);
        animator.Update(0f);

        Debug.Log("[GK] Celebrate (DiveDirection=4)");
    }

    public void PlayDisappointed()
    {
        if (animator == null) return;

        keeperReactedThisRound = true;
        shotDetectedThisRound = true;

        StopAllCoroutines();

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_DISAPPOINTED);
        animator.Update(0f);

        Debug.Log("[GK] Disappointed (DiveDirection=5)");
    }
}
