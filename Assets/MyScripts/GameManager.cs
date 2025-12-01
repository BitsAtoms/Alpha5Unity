using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;

    [Header("Referencias")]
    public BallController ball;
    public Transform ballStartPoint;
    public Transform player;
    public Transform playerStartPoint;

    [Header("Reglas")]
    public int maxAttempts = 4;
    public float shotTimeout = 2f;

    [Header("Tiempos")]
    public float bannerDuration = 2.0f;

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

    // *** IMPORTANTE: Ahora NO depende de betweenRounds ***
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
            yield break;
        }

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(1.5f);

        Set(uiMessage, "");
        Set(uiScore, "");

        ResetPositions();
        yield return StartCoroutine(Co_StartRound());
    }

    IEnumerator Co_StartRound()
    {
        Debug.Log("[GM] Iniciando ronda...");

        // *** ARREGLADO: YA se puede chutar desde el principio ***
        betweenRounds = false;

        Set(uiAttempts, $"Intento: {attempts + 1}/{maxAttempts}");
        Set(uiMessage, "Listo para chutar");
        Set(uiScore, $"Puntuación: {score}");

        // El mensaje NO bloquea chutar
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
        if (ball && ballStartPoint)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            ball.transform.position = ballStartPoint.position;
            ball.ResetFlags();
        }

        if (player && playerStartPoint)
        {
            var prb = player.GetComponent<Rigidbody>();
            if (prb)
            {
                prb.linearVelocity = Vector3.zero;
                prb.angularVelocity = Vector3.zero;
            }
            player.position = playerStartPoint.position;

            var yaw = player.rotation.eulerAngles.y;
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        if (keeper) keeper.ResetForNewRound();
    }

    void Set(TMP_Text t, string s)
    {
        if (t) t.text = s;
    }
}
