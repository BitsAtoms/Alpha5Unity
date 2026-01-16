using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    // alias para compatibilidad con scripts que usan GameManager.Instance
    public static GameManager Instance => I;

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;

    [Header("Referencias")]
    public BallController ball;

    [Header("Reset Pelota (WASD)")]
    public Transform ballStartPoint; // si lo asignas, la pelota se resetea aquí
    private Vector3 ballStartPos;
    private Quaternion ballStartRot;

    [Header("Reglas")]
    public int maxAttempts = 5;
    public float shotTimeout = 1f;

    [Header("Tiempos")]
    public float bannerDuration = 4.0f;

    int score = 0;
    int attempts = 0;

    bool shotArmed = false;
    float shotTimer = 0f;
    bool betweenRounds = true;
    bool gameOver = false;

    void OnEnable() { I = this; }
    void OnDisable() { if (I == this) I = null; }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();

        // Guardar posición inicial de la pelota para reset (si no hay StartPoint)
        if (ball != null)
        {
            if (ballStartPoint != null)
            {
                ballStartPos = ballStartPoint.position;
                ballStartRot = ballStartPoint.rotation;
            }
            else
            {
                ballStartPos = ball.transform.position;
                ballStartRot = ball.transform.rotation;
            }
        }

        Set(uiMessage, "");
        Set(uiScore, "");
        Set(uiAttempts, "");
    }

    void Start()
    {
        ResetPositions();
        StartCoroutine(Co_StartRound());
    }

    void Update()
    {
        if (gameOver || betweenRounds) return;

        if (shotArmed)
        {
            shotTimer += Time.deltaTime;
            if (shotTimer >= shotTimeout)
                ShotFail();
        }
    }

    // ============================
    //         ESTADO TIRO
    // ============================
    public bool CanShoot()
    {
        bool ok = !betweenRounds && !gameOver;
        Debug.Log($"[GM] CanShoot() = {ok} | betweenRounds={betweenRounds} | gameOver={gameOver} | shotArmed={shotArmed}");
        return ok;
    }

    public void ArmShotWindow()
    {
        if (gameOver)
        {
            Debug.Log("[GM] ArmShotWindow() IGNORADO → Juego terminado");
            return;
        }

        shotArmed = true;
        shotTimer = 0f;
        ball?.ResetFlags();

        Debug.Log("[GM] Ventana de tiro ACTIVADA por 2 segundos");
    }

    // ============================
    //           RESULTADOS
    // ============================
    public void GoalScored()
    {
        Debug.Log($"[GM] GoalScored() | shotArmed={shotArmed}");

        if (!shotArmed)
        {
            Debug.Log("[GM] GoalScored() IGNORADO → shotArmed = false");
            return;
        }

        score++;
        attempts++;

        // IMPORTANTE:
        // Quitamos el popup de puntos aquí para que NO salga al marcar gol.
        // El popup SOLO debe salir desde AddTargetScore() cuando se toca una diana.

        // Portero: si hay GOL → decepción (si tu GK lo tiene)
        var keeperAnim = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeperAnim != null)
            keeperAnim.PlayDisappointed();

        StartCoroutine(Co_RestartRound("¡GOL!"));
    }

    public void ShotFail()
    {
        if (!shotArmed)
        {
            Debug.Log("[GM] ShotFail() IGNORADO → shotArmed = false");
            return;
        }

        attempts++;

        // Portero: si hay FALLO → celebración (si tu GK lo tiene)
        var keeperAnim = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeperAnim != null)
            keeperAnim.PlayCelebrate();

        StartCoroutine(Co_RestartRound("Has fallado"));
    }

    // ============================
    //           RONDAS
    // ============================
    IEnumerator Co_RestartRound(string resultMsg)
    {
        shotArmed = false;
        shotTimer = 0f;
        betweenRounds = true;

        if (attempts >= maxAttempts)
        {
            gameOver = true;

            Set(uiMessage, $"Fin del juego\nPuntuación final: {score}/{maxAttempts}");
            Set(uiScore, "");
            Set(uiAttempts, "");

            yield return new WaitForSeconds(3f);

            // Reiniciar variables internas
            score = 0;
            attempts = 0;
            gameOver = false;
            betweenRounds = true;
            shotArmed = false;
            shotTimer = 0f;

            ResetPositions();

            yield return StartCoroutine(Co_StartRound());
            yield break;
        }

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(3f);

        Set(uiMessage, "");
        Set(uiScore, "");

        // Aviso progresivo
        var prog = FindFirstObjectByType<ProgressiveRoundController>();
        if (prog != null)
            prog.OnNewRound(attempts);

        ResetPositions();
        yield return StartCoroutine(Co_StartRound());
    }

    IEnumerator Co_StartRound()
    {
        Debug.Log("[GM] Iniciando ronda...");

        betweenRounds = false;

        Set(uiAttempts, $"Intento: {attempts + 1}/{maxAttempts}");
        Set(uiMessage, "Listo para chutar");
        Set(uiScore, $"Puntuación: {score}");

        yield return new WaitForSeconds(bannerDuration);

        Set(uiAttempts, "");
        Set(uiMessage, "");
        Set(uiScore, "");

        Debug.Log("[GM] Ronda lista → Se puede chutar");
    }

    // ============================
    //        RESET POSICIONES
    // ============================
    void ResetPositions()
    {
        // Reset de pelota (WASD)
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // reset posición/rotación
            ball.transform.position = ballStartPos;
            ball.transform.rotation = ballStartRot;

            ball.ResetFlags();
        }

        // Reset del portero
        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper)
            keeper.ResetForNewRound();

        // ✅ Resetear dianas por ronda (evita bugs de puntos “fantasma”)
        var targets = FindObjectsByType<TargetScore>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in targets)
            t.ResetForNewRound();
    }

    void Set(TMP_Text t, string s)
    {
        if (t) t.text = s;
    }

    public void ResetRoundExternally()
    {
        if (betweenRounds || gameOver)
            return;

        shotArmed = false;
        StartCoroutine(Co_RestartRound(""));
    }

    // ============================
    // LECTURA SEGURA
    // ============================
    public int GetCurrentAttempt()
    {
        return attempts;
    }

    // ============================
    // PUNTOS POR DIANA
    // ============================
    public void AddTargetScore(int points)
    {
        Debug.Log("[GM] AddTargetScore(" + points + ") CALLED BY:\n" + System.Environment.StackTrace);
        score += points;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.ShowPoints(points);

        Debug.Log($"[GM] Diana alcanzada +{points} puntos");
    }
}
