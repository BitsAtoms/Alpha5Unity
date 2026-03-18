using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public static GameManager Instance => I;

    [Header("Delay tras acción del portero")]
    public float failDelayAfterKeeper = 5f;
    
    [Header("Delay portero tras pulso (segundos)")]
    public float keeperActionDelay = 1f;

    Coroutine keeperActionRoutine;
    

    bool pulseSequenceActive = false;
    float pulseSequenceTimer = 0f;

    [Header("FX Overlay (Gol/Fallo)")]
    public GoalFXOverlayPlayer goalFX;

    [Header("Audio - Música")]
    public AudioSource musicSource;
    public bool playMusicOnlyWhenRoundActive = true;

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
    public float failAfterSeconds = 5f;

    public float exactShotStartTime = 0f;

    // Estado
    GameState state = GameState.WaitingForBall;

    int score = 0;
    int attempts = 0;
    int currentGoalMultiplier = 1;

    bool shotArmed = false;
    float shotTimer = 0f;

    // Start por ronda
    bool startPressedThisRound = false;

    // Portero por timestamp
    bool keeperActionDoneThisRound = false;

    // Contador de ronda (para fail automático)
    float roundTimer = 0f;

    // Cache refs (evita Find cada frame)
    SerialSensorEvent sensorDetector;
    GoalkeeperAutoReact keeper;
    StartGameButton startButtonUI;

    Vector3 ballStartPos;
    Quaternion ballStartRot;

    void OnEnable() 
    { 
        I = this; 
        sensorDetector = FindFirstObjectByType<SerialSensorEvent>();
        if (sensorDetector) sensorDetector.OnSensorTriggered += OnPhysicalSensorHit;
    }

    void OnDisable() 
    { 
        if (I == this) I = null; 
        if (sensorDetector) sensorDetector.OnSensorTriggered -= OnPhysicalSensorHit;
    }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();
        if (!goalFX)
        goalFX = FindFirstObjectByType<GoalFXOverlayPlayer>(FindObjectsInactive.Include);

        if (!musicSource)
        musicSource = GameObject.FindFirstObjectByType<AudioSource>(); // Mejor: arrástralo en Inspector


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

    void OnPhysicalSensorHit()
    {
        if (!startPressedThisRound || keeperActionDoneThisRound) return;
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;

        exactShotStartTime = Time.time;

        Debug.Log("[GM] Evento de Sensor recibido. Iniciando parada.");
        keeperActionDoneThisRound = true;
        ArmShotWindow();

        pulseSequenceActive = true;
        pulseSequenceTimer = 0f;

        if (keeperActionRoutine != null) StopCoroutine(keeperActionRoutine);
        keeperActionRoutine = StartCoroutine(Co_KeeperActionAfterDelay());
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
        if (!keeperActionDoneThisRound && roundTimer >= failAfterSeconds)
        {
            Debug.Log("[GM] Timeout de ronda -> ShotFail(force=true)");
            ShotFail(force: true);
            return;
        }
        // 2.5) Delay después de que el portero actúe (evita fallo inmediato por shotTimeout)

        // 2) Timeout de ventana de tiro (si estaba armado)
        if (shotArmed)
        {
            // ✅ Si acabamos de recibir pulso, NO aplicamos el shotTimeout aún
            if (pulseSequenceActive)
            {
                pulseSequenceTimer += Time.deltaTime;

                // 1) durante el primer segundo: esperamos a que el portero se tire
                if (pulseSequenceTimer < keeperActionDelay)
                    return;

                // 2) después de que se tire, esperamos X segundos antes de fallar
                if (pulseSequenceTimer >= keeperActionDelay + failDelayAfterKeeper)
                {
                    Debug.Log("[GM] Secuencia pulso terminada -> ShotFail(force=true)");
                    pulseSequenceActive = false;
                    ShotFail(force: true);
                    return;
                }

                // mientras estamos en esta ventana (portero ya se tiró pero aún no fallamos),
                // NO usamos shotTimeout del GameConfig
                return;
            }

            // ✅ comportamiento normal (si NO hay secuencia activa)
            shotTimer += Time.deltaTime;
            if (shotTimer >= GetShotTimeout())
            {
                Debug.Log("[GM] Timeout shot window -> ShotFail()");
                ShotFail(force: false);
                return;
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
        pulseSequenceActive = false;
        pulseSequenceTimer = 0f;
        currentGoalMultiplier = 1;

        state = GameState.WaitingForBall;
        if (keeperActionRoutine != null)
        {
            StopCoroutine(keeperActionRoutine);
            keeperActionRoutine = null;
        }
        // Mostrar botón Start + ocultar HUD (lo hace tu StartGameButton.ShowForNewRound)
        if (!startButtonUI)
            startButtonUI = FindFirstObjectByType<StartGameButton>(FindObjectsInactive.Include);

        if (startButtonUI)
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
        pulseSequenceActive = false;

        // 1. Primero las comprobaciones de seguridad
        if (!startPressedThisRound) return;
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;

        // 2. Bloquear dobles
        shotArmed = false;
        shotTimer = 0f;

        // 3. Sumar puntos
        int goalPoints = 10 * currentGoalMultiplier;
        score += goalPoints;
        attempts++;

        // 4. Portero: GOL -> decepción
        if (keeper == null)
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        
        if (keeper)
            keeper.PlayDisappointed(); // ✅ Ahora sí llegará hasta aquí

        // 5. Iniciar la corrutina visual de GOL (solo una vez y al final)
        StartCoroutine(Co_RestartRoundWithFX(true));
    }

    public void ShotFail(bool force)
    {
        Debug.Log($"[GM] ShotFail(force={force}) | shotArmed={shotArmed} | state={state}");
        pulseSequenceActive = false;

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
        if (keeper)
            keeper.PlayCelebrate();

        StartCoroutine(Co_RestartRoundWithFX(false));

    }

    IEnumerator Co_RestartRoundWithFX(bool isGoal)
    {
        state = GameState.ShowingResult;

        // ya no mostramos texto
        Set(uiMessage, "");
        Set(uiScore, "");
        Set(uiAttempts, "");

        // reproducir la animación correcta
        if (goalFX == null)
            goalFX = FindFirstObjectByType<GoalFXOverlayPlayer>(FindObjectsInactive.Include);

        float wait = GetResultDisplayDuration();

        if (goalFX)
        {
            if (isGoal)
            {
                goalFX.PlayGol();
                wait = goalFX.golDuration;
            }
            else
            {
                goalFX.PlayFallo();
                wait = goalFX.falloDuration;
            }
        }

        yield return new WaitForSeconds(wait);

                     
        if (attempts >= GetMaxAttempts())
        {
            state = GameState.EndGame;

            Set(uiMessage, $"Fin del juego\nPuntuación final: {score}/{GetMaxAttempts()}");

            yield return new WaitForSeconds(GetEndGameRestartDelay());

            // Cargar escena de espera
            SceneManager.LoadScene("escenaEspera");

            yield break;
        }   

        var prog = FindFirstObjectByType<ProgressiveRoundController>(FindObjectsInactive.Include);
        if (prog)
            prog.OnNewRound(attempts);

        ResetPositions();
        ShowStartForNewRound();
    }


    // overload para mantener compatibilidad con scripts viejos que llamen ShotFail()
    public void ShotFail()
    {
        ShotFail(force: false);
        StartCoroutine(Co_RestartRoundWithFX(isGoal: false));
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
        if (prog)
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

        Set(uiAttempts, $"Intent: {attempts + 1}/{GetMaxAttempts()}");
        Set(uiMessage, "Pots xutar");
        Set(uiScore, $"Puntuació: {score}");

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
        int round = attempts + 1;
        currentGoalMultiplier = GetTargetMultiplierForRound(round);

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup)
            popup.ShowPoints(currentGoalMultiplier);

        Debug.Log($"[GM] Diana alcanzada en ronda {round} -> multiplicador activado x{currentGoalMultiplier}");
    }
    IEnumerator Co_FailAfterDelay()
    {
        yield return new WaitForSeconds(failDelayAfterKeeper);

        // Seguridad extra: solo fallar si seguimos en estado válido
        if (state == GameState.ReadyToShoot || state == GameState.ShotInProgress)
        {
            Debug.Log("[GM] Delay post-portero terminado -> ShotFail()");
            ShotFail(force: true);
        }
    }

        IEnumerator Co_KeeperActionAfterDelay()
    {
        yield return new WaitForSeconds(keeperActionDelay);

        // Seguridad: solo actúa si seguimos en ronda activa
        if (state == GameState.ReadyToShoot || state == GameState.ShotInProgress)
        {
            if (keeper == null)
                keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);

            if (keeper)
                keeper.TriggerPerRoundAction();
        }
    }

    int GetTargetMultiplierForRound(int round)
    {
        switch (round)
        {
            case 3: return 2;
            case 4: return 3;
            case 5: return 5;
            default: return 1;
        }
    }
    public void SetGoalMultiplier(int multiplier)
    {
        if (multiplier <= currentGoalMultiplier) return;

        currentGoalMultiplier = multiplier;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup)
            popup.ShowPoints(currentGoalMultiplier);

        Debug.Log($"[GM] Multiplicador de gol activado -> x{currentGoalMultiplier}");
    }

    public void ActivateRoundTargetMultiplier()
    {
        int round = attempts + 1;
        int multiplier = GetTargetMultiplierForRound(round);

        if (multiplier <= currentGoalMultiplier) return;

        currentGoalMultiplier = multiplier;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup)
            popup.ShowPoints(currentGoalMultiplier);

        Debug.Log($"[GM] Diana alcanzada en ronda {round} -> multiplicador x{currentGoalMultiplier}");
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
