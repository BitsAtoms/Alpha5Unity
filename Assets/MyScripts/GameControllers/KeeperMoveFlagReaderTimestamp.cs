using System.IO;
using UnityEngine;

public class KeeperMoveFlagReaderTimestamp : MonoBehaviour
{
    [Header("Archivo en ../Config")]
    public string fileName = "keeper_move.txt";

    [Header("Lectura cada X segundos")]
    public float pollInterval = 0.08f;

    string filePath;

    long lastTimestamp = -1;
    bool primed = false;
    bool pulse = false;

    void Awake()
    {
        filePath = Path.Combine(Application.dataPath, "../Config", fileName);
        Debug.Log("[KEEPER TS] Ruta archivo = " + filePath);
        EnsureFileExists();
    }

    void OnEnable()
    {
        Debug.Log("[KEEPER TS] OnEnable() -> empieza a leer");
        InvokeRepeating(nameof(ForceReadNow), pollInterval, pollInterval);
    }

    void OnDisable()
    {
        Debug.Log("[KEEPER TS] OnDisable() -> deja de leer");
        CancelInvoke(nameof(ForceReadNow));
    }

    void EnsureFileExists()
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "");

            Debug.Log("[KEEPER TS] EnsureFileExists OK");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER TS] Error EnsureFileExists: " + e.Message);
        }
    }

    // ✅ Lee el timestamp actual pero NO genera pulso (para evitar “doble animación” al empezar)
    public void PrimeFromFile()
    {
        try
        {
            if (!File.Exists(filePath)) return;

            string raw = File.ReadAllText(filePath).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                primed = true;
                Debug.Log("[KEEPER TS] PRIMED (archivo vacío) (sin pulso)");
                return;
            }

            // Formatos aceptados:
            // - "123456789" (solo timestamp)
            // - "1,123456789" (value,ts) -> nos quedamos con ts
            string[] parts = raw.Split(',', ';');
            long ts;

            if (parts.Length == 1)
            {
                if (!long.TryParse(parts[0].Trim(), out ts))
                    return;
            }
            else
            {
                if (!long.TryParse(parts[parts.Length - 1].Trim(), out ts))
                    return;
            }

            lastTimestamp = ts;
            primed = true;
            Debug.Log("[KEEPER TS] PRIMED ts=" + lastTimestamp + " (sin pulso)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER TS] PrimeFromFile error: " + e.Message);
        }
    }

    public void ClearPulse()
    {
        pulse = false;
        Debug.Log("[KEEPER TS] ClearPulse()");
    }

    public bool ConsumePulse()
    {
        if (!pulse) return false;
        pulse = false;
        return true;
    }

    // ✅ Lee y si el timestamp cambia -> pulse=true
    public void ForceReadNow()
    {
        if (!File.Exists(filePath)) return;

        string raw = "";
        try
        {
            raw = File.ReadAllText(filePath);
            string txt = raw.Trim();
            if (string.IsNullOrWhiteSpace(txt)) return;

            string[] parts = txt.Split(',', ';');

            long ts;
            if (parts.Length == 1)
            {
                if (!long.TryParse(parts[0].Trim(), out ts)) return;
            }
            else
            {
                if (!long.TryParse(parts[parts.Length - 1].Trim(), out ts)) return;
            }

            // Si aún no está primed, lo primamos sin pulso (seguridad extra)
            if (!primed)
            {
                lastTimestamp = ts;
                primed = true;
                return;
            }

            if (ts == lastTimestamp) return;

            long prev = lastTimestamp;
            lastTimestamp = ts;
            pulse = true;

            Debug.Log($"✅ [KEEPER TS] TIMESTAMP CAMBIÓ: {prev} -> {ts} (PULSE=TRUE)");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KEEPER TS] Error leyendo (raw='{raw}'): {e.Message}");
        }
    }
}
