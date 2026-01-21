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

    int score = 0;
    int attempts = 0;

    bool shotArmed = false;
    float shotTimer = 0f;

    GameState state = GameState.WaitingForBall;

    Vector3 ballStartPos;
    Quaternion ballStartRot;

    // 🔥 AÑADIDO: para empezar con botón Start
    bool started = false;

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

        // 🔥 AÑADIDO
        started = false;
        Debug.Log("[GM] Esperando START...");
    }

    void Start()
    {
        // 🔥 ya NO arranca solo
        // BeginGame() se llamará desde el botón Start
    }

    // 🔥 AÑADIDO: lo llama el botón Start
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
        if (GameSettings.I == null) return;

        if (GameSettings.I.Mode == GameMode.Nino)
        {
            var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
            if (keeper != null)
                keeper.moveSpeed *= 0.7f;
        }
    }

    void Update()
    {
        if (!started) return;

        if (state != GameState.ShotInProgress) return;

        if (shotArmed)
        {
            shotTimer += Time.deltaTime;
            if (shotTimer >= GetShotTimeout())
                ShotFail();
        }
    }

    public bool CanShoot()
    {
        bool ok = (state == GameState.ReadyToShoot || state == GameState.ShotInProgress);
        Debug.Log($"[GM] CanShoot() = {ok} | state={state} | shotArmed={shotArmed}");
        return ok;
    }

    public void ArmShotWindow()
    {
        if (!started) return;

        if (state == GameState.EndGame)
        {
            Debug.Log("[GM] ArmShotWindow() IGNORADO → Juego terminado");
            return;
        }

        if (state != GameState.ReadyToShoot && state != GameState.ShotInProgress)
            return;

        shotArmed = true;
        shotTimer = 0f;

        ball?.ResetFlags();

        state = GameState.ShotInProgress;
        Debug.Log("[GM] Ventana de tiro ACTIVADA");
    }

    public void GoalScored()
    {
        Debug.Log($"[GM] GoalScored() | shotArmed={shotArmed} | state={state}");

        if (!shotArmed)
        {
            Debug.Log("[GM] GoalScored() IGNORADO → shotArmed = false");
            return;
        }

        shotArmed = false;
        shotTimer = 0f;

        score++;
        attempts++;

        var keeperAnim = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeperAnim != null)
            keeperAnim.PlayDisappointed();

        StartCoroutine(Co_RestartRound("¡GOL!"));
    }

    public void ShotFail()
    {
        Debug.Log($"[GM] ShotFail() | shotArmed={shotArmed} | state={state}");

        if (!shotArmed)
        {
            Debug.Log("[GM] ShotFail() IGNORADO → shotArmed = false");
            return;
        }

        shotArmed = false;
        shotTimer = 0f;

        attempts++;

        var keeperAnim = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeperAnim != null)
            keeperAnim.PlayCelebrate();

        StartCoroutine(Co_RestartRound("Has fallado"));
    }

    IEnumerator Co_RestartRound(string resultMsg)
    {
        state = GameState.ShowingResult;

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

            ResetPositions();
            yield return StartCoroutine(Co_StartRound());
            yield break;
        }

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(GetResultDisplayDuration());

        Set(uiMessage, "");
        Set(uiScore, "");

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

        // =========================================================
        // 🔥🔥🔥 AÑADIDO: EN CADA NUEVA RONDA, PORTERO HACE 1 ANIMACIÓN
        // si el TXT está en 1 (externalMoveAllowed=true)
        // =========================================================
       /* var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper != null)
        {
            keeper.TriggerRandomDiveThisRound_NoShotWindow();
        }*/
    }

    void ResetPositions()
    {
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

        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper)
            keeper.ResetForNewRound();

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
        if (!started) return;

        if (state == GameState.ShowingResult || state == GameState.EndGame)
            return;

        shotArmed = false;
        StartCoroutine(Co_RestartRound(""));
    }

    public int GetCurrentAttempt()
    {
        return attempts;
    }

    public void AddTargetScore(int points)
    {
        score += points;

        var popup = FindFirstObjectByType<PointsPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.ShowPoints(points);

        Debug.Log($"[GM] Diana alcanzada +{points} puntos");
    }

    int GetMaxAttempts() => config ? config.maxAttempts : 5;
    float GetShotTimeout() => config ? config.shotTimeout : 1f;
    float GetBannerDuration() => config ? config.bannerDuration : 4f;
    float GetResultDisplayDuration() => config ? config.resultDisplayDuration : 3f;
    float GetEndGameRestartDelay() => config ? config.endGameRestartDelay : 3f;
}
