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
    public float fieldWidth = 10f;  // X
    public float fieldDepth = 5f;   // Z
    public float fieldHeight = 3f;  // Y

    [Header("Origen del campo")]
    public Vector3 fieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Invertir ejes")]
    public bool invertX = true;
    public bool invertY = false;
    public bool invertZ = true;

    [Header("Opciones de suavizado")]
    public bool smoothMovement = true;
    public float smoothSpeed = 20f;

    private Vector3 targetPosition;
    private bool hasValidData = false;

    void Start()
    {
        // ===========================
        // DETECTAR RUTA FINAL
        // ===========================
        string basePath;

#if UNITY_EDITOR
        // CUANDO ESTÁS EN EL EDITOR → usa la carpeta del proyecto
        basePath = Path.Combine(Application.dataPath, "../Tracking");
#else
        // CUANDO ESTÁS EN EL .EXE → usa la carpeta donde está el ejecutable
        basePath = Path.Combine(Application.dataPath, "../Tracking");
#endif

        topFilePath = Path.Combine(basePath, topFileName);
        sideFilePath = Path.Combine(basePath, sideFileName);

        Debug.Log("[RBT] Usando rutas:");
        Debug.Log("[RBT] TOP = " + topFilePath);
        Debug.Log("[RBT] SIDE = " + sideFilePath);

        targetPosition = transform.position;

        StartCoroutine(ReadLoop());
    }

    void Update()
    {
        if (!hasValidData)
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
    }

    IEnumerator ReadLoop()
    {
        while (true)
        {
            ReadAndUpdate();
            yield return new WaitForSeconds(readInterval);
        }
    }

    void ReadAndUpdate()
    {
        float x = 0.5f;
        float z = 0.5f;
        float y = 0.0f;

        // ===================================
        // LEER CAMARA DEL TECHO (X,Z)
        // ===================================
        if (File.Exists(topFilePath))
        {
            try
            {
                string textTop = File.ReadAllText(topFilePath).Trim();
                string[] parts = textTop.Split(',', ';');

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

        // ===================================
        // LEER CAMARA LATERAL (Y)
        // ===================================
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

        // ===================================
        // MAPEAR A UNIDADES DE UNITY
        // ===================================
        float worldX = fieldOrigin.x + x * fieldWidth;
        float worldZ = fieldOrigin.z + z * fieldDepth;
        float worldY = fieldOrigin.y + y * fieldHeight;

        // Evitar que mueva la pelota con valores basura
        if (!(Mathf.Approximately(x, 0f) &&
              Mathf.Approximately(y, 0f) &&
              Mathf.Approximately(z, 0f)))
        {
            hasValidData = true;
            targetPosition = new Vector3(worldX, worldY, worldZ);
        }
    }
}
