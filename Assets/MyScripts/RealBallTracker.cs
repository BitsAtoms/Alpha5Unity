using System.IO;
using System.Globalization;
using System.Collections;
using UnityEngine;

public class RealBallTracker3D : MonoBehaviour
{
    [Header("Archivos que escriben las cámaras")]
    public string fileTop = @"C:\Tracking\AnimationFile_Top.txt";   // X,Z
    public string fileSide = @"C:\Tracking\AnimationFile_Side.txt"; // Y

    [Header("Frecuencia de lectura")]
    public float readInterval = 0.02f;

    [Header("Tamaño del campo en Unity (X,Z,Y)")]
    public float fieldWidth = 10f;  // X
    public float fieldDepth = 5f;   // Z
    public float fieldHeight = 3f;  // Y

    [Header("Origen del campo")]
    public Vector3 fieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Opciones")]
    public bool invertY = false;
    public bool invertZ = true;
    public bool invertX = true;
    public bool smoothMovement = true;
    public float smoothSpeed = 20f;

    private Vector3 targetPosition;
    private bool hasValidData = false;

    void Start()
    {
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

        // CAMARA TECHO (X,Z)
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

                    if (invertX) x = 1f - x;
                    if (invertZ) z = 1f - z;
                }
            }
            catch { }
        }

        // CAMARA LATERAL (Y)
        if (File.Exists(fileSide))
        {
            try
            {
                string textSide = File.ReadAllText(fileSide).Trim();
                y = float.Parse(textSide, CultureInfo.InvariantCulture);

                if (invertY)
                    y = 1f - y;
            }
            catch { }
        }

        // --------------- MAPEADO CORREGIDO ----------------
        float worldX = fieldOrigin.x + (z * fieldWidth);   // ← usamos Z real para mover X Unity
        float worldZ = fieldOrigin.z + (x * fieldDepth);   // ← usamos X real para mover Z Unity
        float worldY = fieldOrigin.y + (y * fieldHeight);
        // --------------------------------------------------

        if (!(Mathf.Approximately(x, 0f) &&
              Mathf.Approximately(y, 0f) &&
              Mathf.Approximately(z, 0f)))
        {
            hasValidData = true;
            targetPosition = new Vector3(worldX, worldY, worldZ);
        }
    }
}
