using System.IO;
using System.Globalization;
using System.Collections;
using UnityEngine;

public class RealBallTracker3D : MonoBehaviour
{
    [Header("Nombres de archivo (NO rutas absolutas)")]
    public string topFileName = "AnimationFile_Top.txt";   // X,Z
    public string sideFileName = "AnimationFile_Side.txt"; // Y

    private string topFilePath;
    private string sideFilePath;

    [Header("Frecuencia de lectura")]
    public float readInterval = 0.02f;

    [Header("Tamaño del campo en Unity (X,Z,Y)")]
    public float fieldWidth = 10f;
    public float fieldDepth = 5f;
    public float fieldHeight = 3f;

    [Header("Origen del campo")]
    public Vector3 fieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Invertir ejes")]
    public bool invertX = true;
    public bool invertY = false;
    public bool invertZ = true;

    [Header("Intercambiar ejes (Swap)")]
    public bool swapXZ = false;
    public bool swapXY = false;
    public bool swapYZ = false;

    [Header("Opciones de suavizado")]
    public bool smoothMovement = true;
    public float smoothSpeed = 20f;

    private Vector3 targetPosition;

    // 🔥 CONTROL DE ESTADOS
    private bool allowTracking = false;
    private bool hasFirstValidData = false;

    // 🔥 POSICIÓN INICIAL FIJA
    private Vector3 fixedStartPosition = new Vector3(1846.75f, 2.25f, 1822.52f);

    // ====================================================
    // 🔥🔥🔥 AÑADIDO — DETECCIÓN DE INICIO DE TIRO (EJE X)
    // ====================================================
    [Header("Detección inicio de tiro")]
    public float shotDetectThresholdX = 0.05f; // 5 cm
    private float startX;
    private bool shotDetected = false;

    void Start()
    {
        string basePath = Path.Combine(Application.dataPath, "../Tracking");
        topFilePath = Path.Combine(basePath, topFileName);
        sideFilePath = Path.Combine(basePath, sideFileName);

        // Colocar pelota en el inicio fijo
        transform.position = fixedStartPosition;
        targetPosition = fixedStartPosition;

        hasFirstValidData = false;
        allowTracking = false;

        // 🔥 AÑADIDO
        startX = fixedStartPosition.x;
        shotDetected = false;

        StartCoroutine(ReadLoop());
        StartCoroutine(EnableTracking());
    }

    void Update()
    {
        if (!allowTracking || !hasFirstValidData)
            return;

        if (smoothMovement)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }

        // ====================================================
        // 🔥🔥🔥 AÑADIDO — DETECTAR MOVIMIENTO EN X
        // ====================================================
        if (!shotDetected)
        {
            float deltaX = Mathf.Abs(transform.position.x - startX);

            if (deltaX > shotDetectThresholdX)
            {
                shotDetected = true;
                Debug.Log("[RBT] Movimiento en X detectado → inicio de tiro");

                if (GameManager.Instance != null && GameManager.Instance.CanShoot())
                {
                    GameManager.Instance.ArmShotWindow();
                }

                var keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
                if (keeper != null)
                {
                    keeper.OnShotDetected(0f);
                }
            }
        }
    }

    IEnumerator ReadLoop()
    {
        while (true)
        {
            ReadAndUpdate();
            yield return new WaitForSeconds(readInterval);
        }
    }

    private IEnumerator EnableTracking()
    {
        yield return new WaitForSeconds(0.3f);
        allowTracking = true;
        Debug.Log("[RBT] Tracking habilitado");
    }

    void ReadAndUpdate()
    {
        if (!allowTracking)
            return;

        float x = -1f;
        float y = -1f;
        float z = -1f;

        if (File.Exists(topFilePath))
        {
            try
            {
                string textTop = File.ReadAllText(topFilePath).Trim();
                var parts = textTop.Split(',', ';');

                if (parts.Length >= 2)
                {
                    x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    z = float.Parse(parts[1], CultureInfo.InvariantCulture);

                    if (invertX) x = 1f - x;
                    if (invertZ) z = 1f - z;
                }
            }
            catch { }
        }

        if (File.Exists(sideFilePath))
        {
            try
            {
                string textSide = File.ReadAllText(sideFilePath).Trim();
                y = float.Parse(textSide, CultureInfo.InvariantCulture);

                if (invertY) y = 1f - y;
            }
            catch { }
        }

        if (x < 0 || y < 0 || z < 0)
            return;

        if (swapXZ) (x, z) = (z, x);
        if (swapXY) (x, y) = (y, x);
        if (swapYZ) (y, z) = (z, y);

        float worldX = fieldOrigin.x + x * fieldWidth;
        float worldZ = fieldOrigin.z + z * fieldDepth;
        float worldY = fieldOrigin.y + y * fieldHeight;

        if (!hasFirstValidData)
        {
            hasFirstValidData = true;
            Debug.Log("[RBT] Primer dato válido detectado");
        }

        targetPosition = new Vector3(worldX, worldY, worldZ);
    }

    // ====================================================
    // RESET PARA NUEVA RONDA
    // ====================================================
    public void ResetBallPosition()
    {
        StopAllCoroutines();

        allowTracking = false;
        hasFirstValidData = false;

        transform.position = fixedStartPosition;
        targetPosition = fixedStartPosition;

        // 🔥 AÑADIDO
        startX = fixedStartPosition.x;
        shotDetected = false;

        StartCoroutine(ReadLoop());
        StartCoroutine(EnableTracking());

        Debug.Log("[RBT] Pelota reseteada al punto inicial.");
        var returnZone = FindFirstObjectByType<BallReturnTrigger>();
            if (returnZone != null)
            {
                returnZone.ResetTrigger();
            }

    }
}
