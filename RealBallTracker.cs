using System.IO;
using System.Globalization;
using System.Collections;
using UnityEngine;

public class RealBallTracker : MonoBehaviour
{
    [Header("Archivo que escribe el programa externo")]
    public string filePath = @"C:\Tracking\AnimationFile.txt";

    [Header("Frecuencia de lectura (segundos)")]
    public float readInterval = 0.02f;

    [Header("Campo en Unity (tamaño en metros/unidades)")]
    public float FieldWidth = 10f;   // X total
    public float FieldHeight = 5f;   // Z total

    [Header("Origen del campo (esquina inferior izquierda)")]
    public Vector3 FieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Opciones")]
    public bool InvertY = true;
    public bool SmoothMovement = true;
    public float SmoothSpeed = 20f;

    private Vector3 targetPos;

    void Start()
    {
        targetPos = transform.position;
        StartCoroutine(ReadLoop());
    }

    void Update()
    {
        if (SmoothMovement)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                Time.deltaTime * SmoothSpeed
            );
        }
        else
        {
            transform.position = targetPos;
        }
    }

    IEnumerator ReadLoop()
    {
        while (true)
        {
            ReadFileAndUpdate();
            yield return new WaitForSeconds(readInterval);
        }
    }

    void ReadFileAndUpdate()
    {
        if (!File.Exists(filePath))
            return;

        string text = "";

        // 🔥 LECTURA COMPATIBLE CON PYTHON (NO BLOQUEA)
        try
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite   // 🔥 PERMITE QUE PYTHON ESCRIBA MIENTRAS UNITY LEE
            ))
            using (StreamReader reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd().Trim();
            }
        }
        catch
        {
            // Si Python está escribiendo justo ahora, nos saltamos este frame.
            return;
        }

        if (string.IsNullOrEmpty(text))
            return;

        string[] p = text.Split(',', ' ', '\t', ';');
        if (p.Length < 3)
            return;

        if (!float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float xNorm)) return;
        if (!float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float yNorm)) return;

        //-------------------------------------------
        // 🔥 NORMALIZACIÓN → POSICIÓN EN UNITY
        //-------------------------------------------
        float unityX = FieldOrigin.x + (xNorm * FieldWidth);
        float unityZ = FieldOrigin.z + ((InvertY ? yNorm : 1f - yNorm) * FieldHeight);
        float unityY = FieldOrigin.y;

        targetPos = new Vector3(unityX, unityY, unityZ);
    }
}
