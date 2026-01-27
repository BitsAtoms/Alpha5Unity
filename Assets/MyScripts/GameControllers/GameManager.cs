using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public static GameManager Instance => I;

    [Header("Config (ScriptableObject)")]
    public GameConfig config; // si es null usa valores por defecto

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;

    [Header("Referencias")]
    public BallController ball;

    [Header("Reset Pelota (WASD)")]
    public Transform ballStartPoint;

    [Header("Regla: si pasan X segundos desde que empieza la ronda y NO hay gol => fallo automático")]
    public float failAfterSeconds = 4f;

    // Estado
    GameState state = GameState.WaitingForBall;

    int score = 0;
    int attempts = 0;

    bool shotArmed = false;
    float shotTimer = 0f;

    // Start por ronda
    bool startPressedThisRound = false;

    // Portero por timestamp
    bool keeperActionDoneThisRound = false;

    // Contador de ronda (para fail automático)
    float roundTimer = 0f;

    // Cache refs (evita Find cada frame)
    KeeperMoveFlagReaderTimestamp keeperReader;
    GoalkeeperAutoReact keeper;
    StartGameButton startButtonUI;

    Vector3 ballStartPos;
    Quaternion ballStartRot;

    void OnEnable() { I = this; }
    void OnDisable() { if (I == this) I = null; }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();

        // Guardar posición inicial pelota (para reset)
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

        // Cache UI start button controller (puede estar en escena como UIController)
        startButtonUI = FindFirstObjectByType<StartGameButton>(FindObjectsInactive.Include);

        // Estado inicial: esperando start
        state = GameState.WaitingForBall;
        Debug.Log("[GM] Esperando START...");
    }

    void Start()
    {
        // Al arrancar: resetea posiciones y muestra botón start (si existe)
        ResetPositions();
        ShowStartForNewRound();
    }

    void Update()
    {
        // 0) Si no estamos en ronda jugable, no hacemos nada
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)
            return;

        // Si aún no pulsaron Start en esta ronda, NO contamos tiempo ni leemos timestamp
        if (!startPressedThisRound)
            return;

        // 1) Fail automático por tiempo desde inicio de ronda
        roundTimer += Time.deltaTime;
        if (roundTimer >= failAfterSeconds)
        {
            Debug.Log("[GM] Timeout de ronda -> ShotFail(force=true)");
            ShotFail(force: true);
            return;
        }

        // 2) Timeout de ventana de tiro (si estaba armado)
        if (shotArmed)
        {
            shotTimer += Time.deltaTime;
            if (shotTimer >= GetShotTimeout())
            {
                Debug.Log("[GM] Timeout shot window -> ShotFail()");
                ShotFail(force: false);
                return;
            }
        }

        // 3) Portero SOLO cuando cambia timestamp (1 acción por ronda)
        if (!keeperActionDoneThisRound)
        {
            if (keeperReader == null)
                keeperReader = FindFirstObjectByType<KeeperMoveFlagReaderTimestamp>(FindObjectsInactive.Include);

            if (keeper == null)
                keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);

            if (keeperReader != null)
            {
                // lee el archivo y si cambió -> pulse=true (lo verás en consola por el reader)
                keeperReader.ForceReadNow();

                // consume el pulso SOLO una vez
                if (keeperReader.ConsumePulse())
                {
                    keeperActionDoneThisRound = true;

                    Debug.Log("✅ [GM] PULSO TIMESTAMP -> portero reacciona + armShotWindow()");
                    ArmShotWindow();

                    if (keeper != null)
                        keeper.TriggerPerRoundAction();
                    else
                        Debug.LogWarning("[GM] keeper == null, no puedo animar portero");
                }
            }
            else
            {
                // Si no hay reader, no habrá portero por timestamp
                // (lo dejamos en silencio para no spamear)
            }
        }
    }

    // ============================
    // START BUTTON (por ronda)
    // ============================
    public void BeginGame()
    {
        startPressedThisRound = true;
        Debug.Log("✅ [GM] Start PRESIONADO en esta ronda");

        // Inicia visualmente la ronda
        StopAllCoroutines();
        StartCoroutine(Co_StartRound());
    }

    void ShowStartForNewRound()
    {
        startPressedThisRound = false;
        keeperActionDoneThisRound = false;
        shotArmed = false;
        shotTimer = 0f;
        roundTimer = 0f;

        state = GameState.WaitingForBall;

        // Mostrar botón Start + ocultar HUD (lo hace tu StartGameButton.ShowForNewRound)
        if (startButtonUI == null)
            startButtonUI = FindFirstObjectByType<StartGameButton>(FindObjectsInactive.Include);

        if (startButtonUI != null)
            startButtonUI.ShowForNewRound();

        Debug.Log("[GM] Esperando START...");
    }

    // ============================
    //         ESTADO TIRO
    // ============================
    public bool CanShoot()
    {
        bool ok = (state == GameState.ReadyToShoot || state == GameState.ShotInProgress)
                  && !state.Equals(GameState.ShowingResult)
                  && !state.Equals(GameState.EndGame)
                  && startPressedThisRound;

        // (si quieres ver esto en consola descomenta)
        // Debug.Log($"[GM] CanShoot()={ok} state={state} startPressed={startPressedThisRound} shotArmed={shotArmed}");

        return ok;
    }

    public void ArmShotWindow()
    {
        if (state == GameState.EndGame) return;

        // Armar ventana tiro
        shotArmed = true;
        shotTimer = 0f;
        ball?.ResetFlags();

        state = GameState.ShotInProgress;

        Debug.Log("[GM] Ventana de tiro ACTIVADA (shotArmed=true)");
    }

    // ============================
    //           RESULTADOS
    // ============================
    public void GoalScored()
    {
        Debug.Log($"[GM] GoalScored() | shotArmed={shotArmed} | state={state}");

        // Permitimos gol sólo si la ronda está en juego y Start fue pulsado
        if (!startPressedThisRound) return;
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;

        // Si quieres exigir que solo cuente gol cuando shotArmed=true, deja esto:
        // if (!shotArmed) return;

        // Bloquear dobles
        shotArmed = false;
        shotTimer = 0f;

        score++;
        attempts++;

        // Portero: GOL -> decepción (si lo tienes)
        if (keeper == null)
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        if (keeper != null)
            keeper.PlayDisappointed();

        StartCoroutine(Co_RestartRound("¡GOL!"));
    }

    public void ShotFail(bool force)
    {
        Debug.Log($"[GM] ShotFail(force={force}) | shotArmed={shotArmed} | state={state}");

        if (!startPressedThisRound) return;
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;

        // Si no es forzado y no estaba armado, no contamos
        if (!force && !shotArmed)
            return;

        // Bloquear dobles
        shotArmed = false;
        shotTimer = 0f;

        attempts++;

        // Portero: fallo -> celebra (si lo tienes)
        if (keeper == null)
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        if (keeper != null)
            keeper.PlayCelebrate();

        StartCoroutine(Co_RestartRound("Has fallado"));
    }

    // overload para mantener compatibilidad con scripts viejos que llamen ShotFail()
    public void ShotFail()
    {
        ShotFail(force: false);
    }

    // ============================
    //           RONDAS
    // ============================
    IEnumerator Co_RestartRound(string resultMsg)
    {
        state = GameState.ShowingResult;

        // Mostrar resultado
        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(GetResultDisplayDuration());

        Set(uiMessage, "");
        Set(uiScore, "");
        Set(uiAttempts, "");

        // Fin del juego
        if (attempts >= GetMaxAttempts())
        {
            state = GameState.EndGame;

            Set(uiMessage, $"Fin del juego\nPuntuación final: {score}/{GetMaxAttempts()}");
            yield return new WaitForSeconds(GetEndGameRestartDelay());

            // Reset completo
            score = 0;
            attempts = 0;

            ResetPositions();
            ShowStartForNewRound();
            yield break;
        }

        // Progresivo (si lo usas)
        var prog = FindFirstObjectByType<ProgressiveRoundController>(FindObjectsInactive.Include);
        if (prog != null)
            prog.OnNewRound(attempts);

        // Reset posiciones para nueva ronda
        ResetPositions();

        // ✅ importante: aquí NO empezamos automáticamente
        // mostramos Start otra vez, y la ronda arranca cuando pulses el botón.
        ShowStartForNewRound();
    }

    IEnumerator Co_StartRound()
    {
        // Al pulsar Start, empezamos ronda real
        state = GameState.ReadyToShoot;

        keeperActionDoneThisRound = false;
        shotArmed = false;
        shotTimer = 0f;
        roundTimer = 0f;

        Set(uiAttempts, $"Intento: {attempts + 1}/{GetMaxAttempts()}");
        Set(uiMessage, "Listo para chutar");
        Set(uiScore, $"Puntuación: {score}");

        yield return new WaitForSeconds(GetBannerDuration());

        Set(uiAttempts, "");
        Set(uiMessage, "");
        Set(uiScore, "");

        Debug.Log("[GM] Ronda lista -> Se puede chutar");
    }

    // ============================
    //        RESET POSICIONES
    // ============================
    void ResetPositions()
    {
        // Reset pelota (WASD)
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

        // Reset portero
        if (keeper == null)
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);

        if (keeper)
            keeper.ResetForNewRound();

        // Reset dianas por ronda
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
        if (state == GameState.ShowingResult || state == GameState.EndGame)
            return;

        shotArmed = false;
        StopAllCoroutines();
        StartCoroutine(Co_RestartRound(""));
    }

    // ============================
    // LECTURA SEGURA
    // ============================
    public int GetCurrentAttempt() => attempts;

    // ============================
    // PUNTOS POR DIANA
    // ============================
    public void AddTargetScore(int points)
    {
        score += points;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.ShowPoints(points);

        Debug.Log($"[GM] Diana alcanzada +{points} puntos");
    }

    // ============================
    // Config getters (no rompe si config es null)
    // ============================
    int GetMaxAttempts() => config ? config.maxAttempts : 5;
    float GetShotTimeout() => config ? config.shotTimeout : 1f;
    float GetBannerDuration() => config ? config.bannerDuration : 4f;
    float GetResultDisplayDuration() => config ? config.resultDisplayDuration : 3f;
    float GetEndGameRestartDelay() => config ? config.endGameRestartDelay : 3f;
}
