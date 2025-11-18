
using System.IO;
using System.Globalization;
using System.Collections;
using UnityEngine;

public class RealBallTracker : MonoBehaviour
{
    [Header("Archivo que escribe el programa externo")]
    public string filePath = @"C:\Tracking\AnimationFile.txt";

    [Header("Frecuencia de lectura (segundos)")]
    public float readInterval = 0.02f; // ~50 veces por segundo

    [Header("Campo en Unity (tamaño en metros/unidades)")]
    public float fieldWidth = 10f;   // tamaño en X
    public float fieldHeight = 5f;   // tamaño en Z

    [Header("Origen del campo (esquina inferior izquierda)")]
    public Vector3 fieldOrigin = new Vector3(-5f, 0.11f, -2.5f);

    [Header("Opciones")]
    public bool invertY = true;      // a veces el eje Y de cámara está al revés
    public bool smoothMovement = true;
    public float smoothSpeed = 20f;

    private Vector3 targetPosition;

    void Start()
    {
        // Posición inicial = donde estás ahora
        targetPosition = transform.position;
        StartCoroutine(ReadLoop());
    }

    void Update()
    {
        // Movimiento suave hacia la posición objetivo
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
            ReadFileAndUpdateTarget();
            yield return new WaitForSeconds(readInterval);
        }
    }

    void ReadFileAndUpdateTarget()
    {
        if (!File.Exists(filePath))
        {
            // Solo para depurar la primera vez:
            // Debug.LogWarning("No se encuentra el archivo: " + filePath);
            return;
        }

        try
        {
            string text = File.ReadAllText(filePath).Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Admitimos separadores espacio, tab, coma o punto y coma
            string[] parts = text.Split(' ', '\t', ',', ';');
            if (parts.Length < 2) return;

            // Usamos InvariantCulture para forzar el punto decimal (0.35)
            float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[1], CultureInfo.InvariantCulture);

            // Si Y está invertida (por ejemplo, arriba es 0 en la imagen), la damos la vuelta
            if (invertY)
                y = 1f - y;

            // Aquí supongo que x e y están normalizados 0..1
            // Si son píxeles, habría que hacer: x /= widthReal; y /= heightReal;

            float worldX = fieldOrigin.x + x * fieldWidth;
            float worldZ = fieldOrigin.z + y * fieldHeight;
            float worldY = fieldOrigin.y; // altura constante de la pelota

            targetPosition = new Vector3(worldX, worldY, worldZ);
        }
        catch (IOException)
        {
            // A veces el otro programa puede estar escribiendo justo cuando leemos
            // y eso lanza una excepción; la ignoramos
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error leyendo AnimationFile: " + e.Message);
        }
    }
}
