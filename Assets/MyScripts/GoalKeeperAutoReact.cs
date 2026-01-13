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
    // 🔥 AÑADIDO: valores para animaciones por resultado
    // ====================================================
    private const int DIR_IDLE = 0;
    private const int DIR_LEFT = 1;
    private const int DIR_RIGHT = 2;
    private const int DIR_JUMP = 3;
    private const int DIR_CELEBRATE = 4;     // jugador falla
    private const int DIR_DISAPPOINTED = 5;  // jugador marca gol

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

                    // ✅ reaccionar una sola vez por ronda
                    TriggerKeeperReactionOnce();
                }
            }

            lastBallPos = ball.position;
            timer = 0f;
        }
    }

    private void TriggerKeeperReactionOnce()
    {
        if (keeperReactedThisRound) return;
        keeperReactedThisRound = true;

        ElegirMovimiento();
    }

    private void ElegirMovimiento()
    {
        int r = Random.Range(0, 3);

        int diveValue = DIR_IDLE;
        if (r == 0) diveValue = DIR_LEFT;
        else if (r == 2) diveValue = DIR_RIGHT;
        else diveValue = DIR_IDLE;

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.SetInteger(DiveDir, diveValue);
            animator.Update(0f);
            Debug.Log("[GK] DiveDirection=" + diveValue);
        }

        if (moverBase)
        {
            StopAllCoroutines();
            StartCoroutine(MoverBaseExtra(diveValue));
        }
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
        if (keeperReactedThisRound) return;

        shotDetectedThisRound = true;

        if (GameManager.I != null)
            GameManager.I.ArmShotWindow();

        TriggerKeeperReactionOnce();
    }

    // ====================================================
    // 🔥🔥🔥 AÑADIDO: ANIMACIONES POR RESULTADO
    // ====================================================

    // Cuando el jugador FALLA → portero celebra
 // Cuando el jugador FALLA → portero celebra
    public void PlayCelebrate()
    {
        if (animator == null) return;

        // 🔥 fuerza animación de resultado aunque ya haya reaccionado esta ronda
        keeperReactedThisRound = true;
        shotDetectedThisRound = true;

        StopAllCoroutines(); // evita que un movimiento anterior pise la pose

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_CELEBRATE);
        animator.Update(0f);

        Debug.Log("[GK] Celebrate (DiveDirection=4)");
    }

    // Cuando el jugador MARCA GOL → portero decepcionado
    public void PlayDisappointed()
    {
        if (animator == null) return;

        // 🔥 fuerza animación de resultado aunque ya haya reaccionado esta ronda
        keeperReactedThisRound = true;
        shotDetectedThisRound = true;

        StopAllCoroutines(); // evita que un movimiento anterior pise la pose

        animator.applyRootMotion = true;
        animator.SetInteger(DiveDir, DIR_DISAPPOINTED);
        animator.Update(0f);

        Debug.Log("[GK] Disappointed (DiveDirection=5)");
    }
}
