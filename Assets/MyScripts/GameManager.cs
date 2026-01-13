using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    // 🔥 AÑADIDO: alias para que LidarReceiver pueda usar GameManager.Instance
    public static GameManager Instance => I;

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;


    [Header("Reglas")]
    public int maxAttempts = 5;
    public float shotTimeout = 1f;

    [Header("Tiempos")]
    public float bannerDuration = 4.0f;
    public BallController ball;


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
        var popup = FindFirstObjectByType<PointsPopup>();
        if (popup != null)
            popup.ShowPoints(5);

        // 🔥🔥🔥 AÑADIDO: si hay GOL → portero animación de decepción
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

        // 🔥🔥🔥 AÑADIDO: si hay FALLO → portero animación de celebración
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

            // Esperamos unos segundos antes de reiniciar todo
            yield return new WaitForSeconds(3f);

            // 🔥 Reiniciar variables internas del GameManager
            score = 0;
            attempts = 0;
            gameOver = false;
            betweenRounds = true;
            shotArmed = false;
            shotTimer = 0f;

            // 🔥 Reset posiciones en el campo
            ResetPositions();

            // 🔥 Inicio del juego desde la primera ronda
            yield return StartCoroutine(Co_StartRound());
            yield break;
        }

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(3f);

        Set(uiMessage, "");
        Set(uiScore, "");

        // 🔥🔥🔥 AÑADIDO (NO MODIFICA NADA EXISTENTE)
        // Avisamos al sistema progresivo en qué tiro vamos
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
        // Reset solo para BallController (para colisiones de gol)
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            ball.ResetFlags();
        }

        // Reset de la pelota real (tracking)
        var tracker = FindFirstObjectByType<RealBallTracker3D>();
        if (tracker)
            tracker.ResetBallPosition();

        // Reset del portero
        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper)
            keeper.ResetForNewRound();
    }

    void Set(TMP_Text t, string s)
    {
        if (t) t.text = s;
    }

    public void ResetRoundExternally()
    {
        if (betweenRounds || gameOver)
            return;

        // Esto impide que vuelva a marcar gol o fallo accidentalmente
        shotArmed = false;

        StartCoroutine(Co_RestartRound(""));
    }

    // ============================
    // 🔥🔥🔥 AÑADIDO (LECTURA SEGURA)
    // Permite saber en qué tiro vamos
    // ============================
    public int GetCurrentAttempt()
    {
        return attempts;
    }

    // ============================
    // 🔥 AÑADIDO: PUNTOS POR DIANA
    // ============================
    public void AddTargetScore(int points)
    {
        score += points;

        var popup = FindFirstObjectByType<PointsPopup>();
        if (popup != null)
            popup.ShowPoints(points);

        Debug.Log($"[GM] Diana alcanzada +{points} puntos");
    }
}
