using System.IO;
using System.Globalization;
using System.Collections;
using UnityEngine;

public class RealBallTracker3D : MonoBehaviour
{
    [Header("Archivos que escriben las cámaras")]
    public string fileTop = @"C:\Tracking\AnimationFile_Top.txt";   // X,Z
    public string fileSide = @"C:\Tracking\AnimationFile_Side.txt"; // Y

<<<<<<< HEAD
    [Header("Frecuencia de lectura")]
    public float readInterval = 0.02f;

    [Header("Tamaño del campo en Unity (X,Z,Y)")]
    public float fieldWidth  = 10f; // X
    public float fieldDepth  = 5f;  // Z
    public float fieldHeight = 3f;  // Y

    [Header("Origen del campo")]
    public Vector3 fieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Opciones")]
    public bool invertY = false;
    public bool invertZ = false;
=======
    [Header("Frecuencia de lectura (segundos)")]
    public float readInterval = 0.02f;

    [Header("Campo en Unity (metros/unidades)")]
    public float fieldWidth = 10f;   // -5 a +5
    public float fieldDepth = 5f;    // -2.5 a +2.5

    [Header("Centro del campo en Unity")]
    public Vector3 fieldCenter = new Vector3(0f, 0.11f, 0f);

    [Header("Rango real medido (metros)")]
    public float realXRange = 0.12f;   // 12 cm (de -0.06 a +0.06)
    public float realZMin = 0.40f;
    public float realZMax = 0.80f;     // 40–80 cm de distancia

    [Header("Opciones de suavidad")]
>>>>>>> cb578a07c41ede490fb74372f6529c6fee88ee39
    public bool smoothMovement = true;
    public float smoothSpeed = 20f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.position;
        StartCoroutine(ReadLoop());
    }

    void Update()
    {
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
<<<<<<< HEAD
        float x = 0.5f;
        float z = 0.5f;
        float y = 0.0f;

        // ==========================
        // LEER CAMARA DEL TECHO (X,Z)
        // ==========================
        if (File.Exists(fileTop))
        {
            try
            {
                string textTop = File.ReadAllText(fileTop).Trim();
                string[] parts = textTop.Split(',', ';');

                if (parts.Length >= 2)
                {
                    x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    z = float.Parse(parts[1], CultureInfo.InvariantCulture);

                    if (invertZ)
                        z = 1f - z;
                }
            }
            catch { }
        }
=======
        if (!File.Exists(filePath))
            return;
>>>>>>> cb578a07c41ede490fb74372f6529c6fee88ee39

        // ==========================
        // LEER CAMARA LATERAL (Y)
        // ==========================
        if (File.Exists(fileSide))
        {
            try
            {
                string textSide = File.ReadAllText(fileSide).Trim();
                y = float.Parse(textSide, CultureInfo.InvariantCulture);

<<<<<<< HEAD
                if (invertY)
                    y = 1f - y;
            }
            catch { }
        }

        // ==========================
        // MAPEAR A UNIDADES DE UNITY
        // ==========================
        float worldX = fieldOrigin.x + x * fieldWidth;
        float worldZ = fieldOrigin.z + z * fieldDepth;
        float worldY = fieldOrigin.y + y * fieldHeight;

        targetPosition = new Vector3(worldX, worldY, worldZ);
=======
            string[] parts = text.Split(' ', '\t', ',', ';');
            if (parts.Length < 3) return;

            // Valores reales del Python
            float X = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float Z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            debug.log(X, Y, Z);
            // ⚠️ SI NO HAY PELOTA (Python manda todo 0)
            if (Mathf.Approximately(X, 0f) && Mathf.Approximately(Y, 0f) && Mathf.Approximately(Z, 0f))
            {
                return; // ❌ No mover la pelota
            }

            // --- NORMALIZACIÓN ---
            float xNorm = Mathf.Clamp(X / (realXRange / 2f), -1f, 1f);
            float zNorm = Mathf.InverseLerp(realZMin, realZMax, Z); // 0..1

            // --- MAPEADO A UNITY ---
            float unityX = fieldCenter.x + xNorm * (fieldWidth / 2f);
            float unityZ = fieldCenter.z + (zNorm - 0.5f) * fieldDepth;

            float unityY = fieldCenter.y;

            targetPosition = new Vector3(unityX, unityY, unityZ);
        }
        catch { }
>>>>>>> cb578a07c41ede490fb74372f6529c6fee88ee39
    }
}
