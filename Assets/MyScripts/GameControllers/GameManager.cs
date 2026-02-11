using UnityEngine;
using TMPro;
using System.Collections;

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
    bool keeperDelayActive = false;
    float keeperDelayTimer = 0f;

    [Header("FX Overlay (Gol/Fallo)")]
    public GoalFXOverlayPlayer goalFX;

    [Header("Audio - Música")]
    public AudioSource musicSource;
    public bool playMusicOnlyWhenRoundActive = true;

    [Header("Config (ScriptableObject)")]
    public GameConfig config; 

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

    GameState state = GameState.WaitingForBall;
    int score = 0;
    int attempts = 0;
    bool shotArmed = false;
    float shotTimer = 0f;
    bool startPressedThisRound = false;
    bool keeperActionDoneThisRound = false;
    float roundTimer = 0f;

    // Cambiado al nuevo lector directo
    SerialPresenceReader keeperReader; 
    GoalkeeperAutoReact keeper;
    StartGameButton startButtonUI;

    Vector3 ballStartPos;
    Quaternion ballStartRot;

    void OnEnable() { I = this; }
    void OnDisable() { if (I == this) I = null; }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();
        if (goalFX == null) goalFX = FindFirstObjectByType<GoalFXOverlayPlayer>(FindObjectsInactive.Include);
        if (musicSource == null) musicSource = GameObject.FindFirstObjectByType<AudioSource>();

        if (ball != null)
        {
            if (ballStartPoint != null) { ballStartPos = ballStartPoint.position; ballStartRot = ballStartPoint.rotation; }
            else { ballStartPos = ball.transform.position; ballStartRot = ball.transform.rotation; }
        }

        Set(uiMessage, ""); Set(uiScore, ""); Set(uiAttempts, "");
        startButtonUI = FindFirstObjectByType<StartGameButton>(FindObjectsInactive.Include);
    }

    void Start() { ResetPositions(); ShowStartForNewRound(); }

    void Update()
    {
        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress) return;
        if (!startPressedThisRound) return;

        roundTimer += Time.deltaTime;

        if (!keeperActionDoneThisRound && roundTimer >= failAfterSeconds)
        {
            ShotFail(force: true);
            return;
        }

        if (keeperDelayActive)
        {
            keeperDelayTimer += Time.deltaTime;
            if (keeperDelayTimer >= failDelayAfterKeeper) keeperDelayActive = false;
            else return;
        }

        if (shotArmed)
        {
            if (pulseSequenceActive)
            {
                pulseSequenceTimer += Time.deltaTime;
                if (pulseSequenceTimer < keeperActionDelay) return;
                if (pulseSequenceTimer >= keeperActionDelay + failDelayAfterKeeper)
                {
                    pulseSequenceActive = false;
                    ShotFail(force: true);
                    return;
                }
                return;
            }

            shotTimer += Time.deltaTime;
            if (shotTimer >= GetShotTimeout()) { ShotFail(force: false); return; }
        }

        if (!keeperActionDoneThisRound)
        {
            if (keeperReader == null) keeperReader = FindFirstObjectByType<SerialPresenceReader>(FindObjectsInactive.Include);
            if (keeper == null) keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);

            if (keeperReader != null && keeperReader.ConsumePulse())
            {
                keeperActionDoneThisRound = true;
                ArmShotWindow();
                pulseSequenceActive = true;
                pulseSequenceTimer = 0f;
                if (keeperActionRoutine != null) StopCoroutine(keeperActionRoutine);
                keeperActionRoutine = StartCoroutine(Co_KeeperActionAfterDelay());
            }
        }
    }

    public void BeginGame()
    {
        startPressedThisRound = true;
        StopAllCoroutines();
        StartCoroutine(Co_StartRound());
    }

    void ShowStartForNewRound()
    {
        startPressedThisRound = false;
        keeperActionDoneThisRound = false;
        shotArmed = false; shotTimer = 0f; roundTimer = 0f;
        pulseSequenceActive = false; pulseSequenceTimer = 0f;
        state = GameState.WaitingForBall;
        if (keeperActionRoutine != null) { StopCoroutine(keeperActionRoutine); keeperActionRoutine = null; }
        if (startButtonUI == null) startButtonUI = FindFirstObjectByType<StartGameButton>(FindObjectsInactive.Include);
        if (startButtonUI != null) startButtonUI.ShowForNewRound();
    }

    public bool CanShoot() => (state == GameState.ReadyToShoot || state == GameState.ShotInProgress) && !state.Equals(GameState.ShowingResult) && !state.Equals(GameState.EndGame) && startPressedThisRound;

    public void ArmShotWindow()
    {
        if (state == GameState.EndGame) return;
        shotArmed = true; shotTimer = 0f;
        ball?.ResetFlags();
        state = GameState.ShotInProgress;
    }

    public void GoalScored()
    {
        if (!startPressedThisRound || (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)) return;
        shotArmed = false; score++; attempts++;
        if (keeper == null) keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        if (keeper != null) keeper.PlayDisappointed();
        StartCoroutine(Co_RestartRoundWithFX(true));
    }

    public void ShotFail(bool force)
    {
        if (!startPressedThisRound || (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)) return;
        if (!force && !shotArmed) return;
        shotArmed = false; attempts++;
        if (keeper == null) keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        if (keeper != null) keeper.PlayCelebrate();
        StartCoroutine(Co_RestartRoundWithFX(false));
    }

    public void ShotFail() => ShotFail(force: false);

    IEnumerator Co_RestartRoundWithFX(bool isGoal)
    {
        state = GameState.ShowingResult;
        Set(uiMessage, ""); Set(uiScore, ""); Set(uiAttempts, "");
        if (goalFX == null) goalFX = FindFirstObjectByType<GoalFXOverlayPlayer>(FindObjectsInactive.Include);

        float wait = GetResultDisplayDuration();
        if (goalFX != null) { if (isGoal) { goalFX.PlayGol(); wait = goalFX.golDuration; } else { goalFX.PlayFallo(); wait = goalFX.falloDuration; } }
        yield return new WaitForSeconds(wait);

        if (attempts >= GetMaxAttempts())
        {
            state = GameState.EndGame;
            Set(uiMessage, $"Fin del juego\nPuntuación: {score}/{GetMaxAttempts()}");
            yield return new WaitForSeconds(GetEndGameRestartDelay());
            score = 0; attempts = 0; ResetPositions(); ShowStartForNewRound();
            yield break;
        }
        ResetPositions(); ShowStartForNewRound();
    }

    public void ResetRoundExternally()
    {
        if (state == GameState.ShowingResult || state == GameState.EndGame) return;
        shotArmed = false; StopAllCoroutines(); StartCoroutine(Co_RestartRound(""));
    }

    IEnumerator Co_RestartRound(string msg)
    {
        state = GameState.ShowingResult;
        if (!string.IsNullOrEmpty(msg)) Set(uiMessage, msg);
        yield return new WaitForSeconds(GetResultDisplayDuration());
        ResetPositions(); ShowStartForNewRound();
    }

    IEnumerator Co_StartRound()
    {
        state = GameState.ReadyToShoot;
        Set(uiAttempts, $"Intento: {attempts + 1}/{GetMaxAttempts()}");
        Set(uiScore, $"Puntuación: {score}");
        yield return new WaitForSeconds(GetBannerDuration());
        Set(uiAttempts, ""); Set(uiMessage, ""); Set(uiScore, "");
    }

    // Busca esta función dentro de tu GameManager y sustitúyela:
    void ResetPositions()
    {
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            ball.transform.SetPositionAndRotation(ballStartPos, ballStartRot);
            ball.ResetFlags();
        }

        if (keeper == null) keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
        if (keeper) keeper.ResetForNewRound();

        // LÍNEA CORREGIDA AQUÍ:
        var targets = FindObjectsByType<TargetScore>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in targets) t.ResetForNewRound();
    }

    public void AddTargetScore(int points) { score += points; Set(uiScore, $"Puntuación: {score}"); }
    public int GetCurrentAttempt() => attempts;
    void Set(TMP_Text t, string s) { if (t) t.text = s; }

    IEnumerator Co_KeeperActionAfterDelay()
    {
        yield return new WaitForSeconds(keeperActionDelay);
        if (state == GameState.ReadyToShoot || state == GameState.ShotInProgress)
        {
            if (keeper == null) keeper = FindFirstObjectByType<GoalkeeperAutoReact>(FindObjectsInactive.Include);
            if (keeper != null) keeper.TriggerPerRoundAction();
        }
    }

    // Getters
    int GetMaxAttempts() => config ? config.maxAttempts : 5;
    float GetShotTimeout() => config ? config.shotTimeout : 1f;
    float GetBannerDuration() => config ? config.bannerDuration : 4f;
    float GetResultDisplayDuration() => config ? config.resultDisplayDuration : 3f;
    float GetEndGameRestartDelay() => config ? config.endGameRestartDelay : 3f;
}