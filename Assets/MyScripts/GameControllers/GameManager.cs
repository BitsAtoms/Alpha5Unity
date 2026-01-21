using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public static GameManager Instance => I;

    [Header("Config")]
    public GameConfig config;

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;

    [Header("Referencias")]
    public BallController ball;

    [Header("Reset Pelota (WASD)")]
    public Transform ballStartPoint;

    [Header("Timeout de ronda (resultado por TXT)")]
    public float roundTimeoutSeconds = 2f;

    int score = 0;
    int attempts = 0;

    bool shotArmed = false;
    float shotTimer = 0f;

    GameState state = GameState.WaitingForBall;

    Vector3 ballStartPos;
    Quaternion ballStartRot;

    bool started = false;

    // evita doble resolución (gol + timeout, etc.)
    bool roundResolved = false;
    Coroutine coRoundTimeout;

    void OnEnable() { I = this; }
    void OnDisable() { if (I == this) I = null; }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();

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

        state = GameState.WaitingForBall;

        started = false;
        roundResolved = false;
        shotArmed = false;
        shotTimer = 0f;

        Debug.Log("[GM] Esperando START...");
    }

    void Start()
    {
        // No arrancamos automáticamente: BeginGame() lo llama el botón START
    }

    // =========================================================
    // START DESDE BOTÓN
    // =========================================================
    public void BeginGame()
    {
        if (started) return;
        started = true;

        Debug.Log("[GM] BeginGame() -> empezando partida");

        ApplyModeIfAny();
        ResetPositions();
        StartCoroutine(Co_StartRound());
    }

    void ApplyModeIfAny()
    {
        // Si tienes GameSettings / GameMode en tu proyecto, aquí puedes ajustar dificultad.
        // Si no lo usas, no pasa nada, se queda vacío.
        // Ejemplo (si existe en tu proyecto):
        // if (GameSettings.I != null && GameSettings.I.Mode == GameMode.Nino) { ... }
    }

    // =========================================================
    // UPDATE (si aún quieres usar shotTimeout clásico, se mantiene)
    // =========================================================
    void Update()
    {
        if (!started) return;

        // Si mantienes el timeout clásico cuando se arma el tiro:
        if (state != GameState.ShotInProgress) return;

        if (shotArmed)
        {
            shotTimer += Time.deltaTime;
            if (shotTimer >= GetShotTimeout())
            {
                // Esto sería fallo “clásico”; pero el resultado principal lo decide el TXT.
                // Puedes dejarlo o quitarlo. Lo dejamos por compatibilidad.
                ShotFail();
            }
        }
    }

    // 🔥 AÑADIDO: lo usa GoalResultFlagReader para decidir si lee o no
    public bool IsShotArmed()
    {
        return shotArmed;
    }

    // =========================================================
    // ARMAR VENTANA DE TIRO (si algún sistema la usa)
    // =========================================================
    public void ArmShotWindow()
    {
        if (!started) return;

        if (state == GameState.EndGame) return;
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;

        shotArmed = true;
        shotTimer = 0f;

        ball?.ResetFlags();

        state = GameState.ShotInProgress;
        Debug.Log("[GM] Ventana de tiro ACTIVADA (shotArmed=true)");
    }

    // =========================================================
    // RESULTADOS (AHORA DEBEN FUNCIONAR DESDE TXT)
    // =========================================================
    public void GoalScored()
    {
        if (!started) return;

        // Si ya se resolvió (por timeout u otro), ignorar
        if (roundResolved) return;

        // Solo aceptar gol si estamos en ronda jugable
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)
            return;

        roundResolved = true;
        StopRoundTimeout();

        Debug.Log("[GM] GoalScored() (por TXT o colisión)");

        shotArmed = false;
        shotTimer = 0f;

        score++;
        attempts++;

        // Portero: GOL -> decepción
        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper != null)
            keeper.PlayDisappointed();

        StartCoroutine(Co_RestartRound("¡GOL!"));
    }

    public void ShotFail()
    {
        if (!started) return;

        if (roundResolved) return;

        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)
            return;

        roundResolved = true;
        StopRoundTimeout();

        Debug.Log("[GM] ShotFail() (por TXT o timeout)");

        shotArmed = false;
        shotTimer = 0f;

        attempts++;

        // Portero: FALLO -> celebración
        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper != null)
            keeper.PlayCelebrate();

        StartCoroutine(Co_RestartRound("Has fallado"));
    }

    // =========================================================
    // TIMEOUT DE RONDA (4s si no llega resultado)
    // =========================================================
    void StartRoundTimeout()
    {
        StopRoundTimeout();
        roundResolved = false;
        coRoundTimeout = StartCoroutine(Co_RoundTimeout());
        Debug.Log("[GM] RoundTimeout START -> " + roundTimeoutSeconds + "s");
    }

    void StopRoundTimeout()
    {
        if (coRoundTimeout != null)
        {
            StopCoroutine(coRoundTimeout);
            coRoundTimeout = null;
            Debug.Log("[GM] RoundTimeout STOP");
        }
    }

    IEnumerator Co_RoundTimeout()
    {
        // Realtime para que funcione aunque haya timeScale o contadores externos
        yield return new WaitForSecondsRealtime(roundTimeoutSeconds);

        if (roundResolved) yield break;

        Debug.Log("[GM] RoundTimeout -> FAIL automático (no hubo resultado TXT)");

        // Forzamos fallo aunque no exista shotArmed
        ShotFail();
    }

    // =========================================================
    // RONDAS
    // =========================================================
    IEnumerator Co_RestartRound(string resultMsg)
    {
        state = GameState.ShowingResult;

        StopRoundTimeout();
        shotArmed = false;
        shotTimer = 0f;

        // Fin del juego (reinicio como antes)
        if (attempts >= GetMaxAttempts())
        {
            state = GameState.EndGame;

            Set(uiMessage, $"Fin del juego\nPuntuación final: {score}/{GetMaxAttempts()}");
            Set(uiScore, "");
            Set(uiAttempts, "");

            yield return new WaitForSeconds(GetEndGameRestartDelay());

            score = 0;
            attempts = 0;

            shotArmed = false;
            shotTimer = 0f;
            roundResolved = false;

            ResetPositions();
            yield return StartCoroutine(Co_StartRound());
            yield break;
        }

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(GetResultDisplayDuration());

        Set(uiMessage, "");
        Set(uiScore, "");

        // Progresivo (si existe)
        var prog = FindFirstObjectByType<ProgressiveRoundController>();
        if (prog != null)
            prog.OnNewRound(attempts);

        ResetPositions();
        yield return StartCoroutine(Co_StartRound());
    }

    IEnumerator Co_StartRound()
    {
        Debug.Log("[GM] Iniciando ronda...");

        state = GameState.ReadyToShoot;

        Set(uiAttempts, $"Intento: {attempts + 1}/{GetMaxAttempts()}");
        Set(uiMessage, "Listo para chutar");
        Set(uiScore, $"Puntuación: {score}");

        yield return new WaitForSeconds(GetBannerDuration());

        Set(uiAttempts, "");
        Set(uiMessage, "");
        Set(uiScore, "");

        Debug.Log("[GM] Ronda lista → Se puede chutar");

        // 🔥 IMPORTANTE: aquí empieza el conteo de 4s para que llegue resultado (TXT)
        StartRoundTimeout();
        // ==============================================
        // 🔥 AÑADIDO: PORTERO POR RONDA SEGÚN keeper_move.txt
        // ==============================================
        var keeperReader = FindFirstObjectByType<KeeperMoveFlagReaderTimestamp>(FindObjectsInactive.Include);
        if (keeperReader != null)
            keeperReader.ForceReadNow(); // lee el 0/1 ahora mismo

        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper != null)
        {
            // Si tu portero tiene método RefreshExternalState, úsalo (si no, no pasa nada)
            // keeper.RefreshExternalState();

            keeper.TriggerPerRoundAction(); // si TXT=0 no hace nada, si TXT=1 hace 1..3
        }

    }

    // =========================================================
    // RESET POSICIONES
    // =========================================================
    void ResetPositions()
    {
        // Pelota WASD
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            ball.transform.position = ballStartPos;
            ball.transform.rotation = ballStartRot;

            ball.ResetFlags();
        }

        // Portero
        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper)
            keeper.ResetForNewRound();

        // Dianas (si existen)
        var targets = FindObjectsByType<TargetScore>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in targets)
            t.ResetForNewRound();

        // Reset flags internos de ronda
        roundResolved = false;
        shotArmed = false;
        shotTimer = 0f;
    }

    void Set(TMP_Text t, string s)
    {
        if (t) t.text = s;
    }

    // Por si otro sistema fuerza reinicio
    public void ResetRoundExternally()
    {
        if (!started) return;

        if (state == GameState.ShowingResult || state == GameState.EndGame)
            return;

        roundResolved = true;
        StopRoundTimeout();

        shotArmed = false;
        StartCoroutine(Co_RestartRound(""));
    }

    public int GetCurrentAttempt()
    {
        return attempts;
    }

    // Puntos por diana
    public void AddTargetScore(int points)
    {
        score += points;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.ShowPoints(points);

        Debug.Log($"[GM] Diana alcanzada +{points} puntos");
    }
        // =====================================
    // COMPATIBILIDAD CON SCRIPTS ANTIGUOS
    // =====================================
    public bool CanShoot()
    {
        // Se puede “chutar” cuando la ronda está activa
        return started &&
            (state == GameState.ReadyToShoot || state == GameState.ShotInProgress) &&
            !roundResolved;
    }


    // =========================================================
    // GETTERS CONFIG (no rompe si config es null)
    // =========================================================
    int GetMaxAttempts() => config ? config.maxAttempts : 5;
    float GetShotTimeout() => config ? config.shotTimeout : 1f;
    float GetBannerDuration() => config ? config.bannerDuration : 4f;
    float GetResultDisplayDuration() => config ? config.resultDisplayDuration : 3f;
    float GetEndGameRestartDelay() => config ? config.endGameRestartDelay : 3f;
}
