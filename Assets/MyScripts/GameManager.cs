using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)] // que se inicialice pronto
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("UI (TMP)")]
    public TMP_Text uiMessage;
    public TMP_Text uiScore;
    public TMP_Text uiAttempts;

    [Header("Refs")]
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

    void OnEnable()
    {
        I = this;
        Debug.Log("[GM] OnEnable -> singleton listo");
    }

    void OnDisable()
    {
        if (I == this) I = null;
        Debug.Log("[GM] OnDisable -> singleton limpiado");
    }

    void Awake()
    {
        if (!ball) ball = FindFirstObjectByType<BallController>();
        Set(uiMessage, ""); Set(uiScore, ""); Set(uiAttempts, "");
        Debug.Log("[GM] Awake()");
    }

    void Start()
    {
        Debug.Log("[GM] Start() -> ResetPositions y Co_StartRound");
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
            {
                ShotFail();
            }
        }
    }

    // === API ===
    public bool CanShoot()
    {
        bool ok = !betweenRounds && !gameOver;
        // Debug.Log($"[GM] CanShoot? {ok} (betweenRounds={betweenRounds}, gameOver={gameOver})");
        return ok;
    }

    public void ArmShotWindow()
    {
        if (!CanShoot())
        {
            Debug.Log("[GM] ArmShotWindow() ignorado: no se puede chutar todavía");
            return;
        }
        shotArmed = true;
        shotTimer = 0f;
        ball?.ResetFlags();
        Debug.Log("[GM] Tiro ARMADO (ventana 2s activa)");
    }

    public void GoalScored()
    {
        Debug.Log($"[GM] GoalScored() llamado | shotArmed={shotArmed} canShoot={CanShoot()}");
        if (!shotArmed || !CanShoot()) return;

        if (ball != null && ball.TouchedKeeper)
        {
            ShotFail();
            return;
        }

        score += 1;
        attempts += 1;
        Debug.Log($"[GM] GOL → score={score} attempts={attempts}");
        StartCoroutine(Co_RestartRound("¡GOL!"));
    }

    public void ShotFail()
    {
        Debug.Log($"[GM] ShotFail() llamado | shotArmed={shotArmed} canShoot={CanShoot()}");
        if (!shotArmed || !CanShoot()) return;

        attempts += 1;
        Debug.Log($"[GM] FALLO → score={score} attempts={attempts}");
        StartCoroutine(Co_RestartRound("Has fallado"));
    }

    IEnumerator Co_RestartRound(string resultMsg)
    {
        shotArmed = false;
        shotTimer = 0f;

        if (attempts >= maxAttempts)
        {
            gameOver = true;
            Set(uiMessage, $"Fin del juego\nPuntuación final: {score}/{maxAttempts}");
            Set(uiScore, ""); Set(uiAttempts, "");
            Debug.Log("[GM] FIN DEL JUEGO");
            yield break;
        }

        betweenRounds = true;

        Set(uiMessage, resultMsg);
        yield return new WaitForSeconds(0.25f);

        Set(uiMessage, ""); Set(uiScore, "");

        ResetPositions();

        yield return StartCoroutine(Co_StartRound());

        betweenRounds = false;
        Debug.Log("[GM] Ronda lista: se puede chutar");
    }

    IEnumerator Co_StartRound()
        {
            Debug.Log("[GM] Co_StartRound -> mostrando banners de inicio");
            Set(uiAttempts, $"Intento: {attempts + 1}/{maxAttempts}");
            Set(uiMessage, "Listo para chutar");
            Set(uiScore, $"Puntuación: {score}");

            yield return new WaitForSeconds(bannerDuration);

            Set(uiAttempts, "");
            Set(uiMessage, "");
            Set(uiScore, "");

            // 👇👇 ARREGLO CLAVE
            betweenRounds = false;       // ahora sí se puede chutar
            Debug.Log("[GM] Co_StartRound -> banners ocultos; ya se puede chutar (betweenRounds=false)");
        }


    void ResetPositions()
    {
        Debug.Log("[GM] ResetPositions()");
        if (ball && ballStartPoint)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            ball.transform.position = ballStartPoint.position;
        }

        if (player && playerStartPoint)
        {
            var prb = player.GetComponent<Rigidbody>();
            if (prb) { prb.linearVelocity = Vector3.zero; prb.angularVelocity = Vector3.zero; }
            player.position = playerStartPoint.position;
            var yaw = player.rotation.eulerAngles.y;
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void Set(TMP_Text t, string s) { if (t) t.text = s; }
}
