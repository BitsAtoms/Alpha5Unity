using System.IO;
using UnityEngine;

public class KeeperMoveFlagReaderTimestamp : MonoBehaviour
{
    [Header("Archivo en ../Config")]
    public string fileName = "keeper_move.txt";

    [Header("Lectura cada X segundos")]
    public float pollInterval = 0.15f;

    [Header("Por defecto si falta/está vacío")]
    public int defaultValue = 0;

    public bool CurrentAllowMove { get; private set; } = false;

    string filePath;
    GoalkeeperAutoReact keeper;

    long lastTimestamp = -1;

    void Awake()
    {
        filePath = Path.Combine(Application.dataPath, "../Config", fileName);
        Debug.Log("[KEEPER TS] Ruta archivo = " + filePath);

        keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
        EnsureFileExists();

        // primera lectura
        ForceReadNow();
    }

    void OnEnable()
    {
        InvokeRepeating(nameof(ForceReadNow), pollInterval, pollInterval);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ForceReadNow));
    }

    void EnsureFileExists()
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Debug.Log("[KEEPER FLAG] 📁 Directorio creado: " + dir);
            }

            if (!File.Exists(filePath))
            {
                // escribimos con timestamp actual
                long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                File.WriteAllText(filePath, $"{defaultValue},{ts}");
                Debug.LogWarning("[KEEPER TS] No existía -> creado: " + defaultValue + "," + ts);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER TS] Error creando/asegurando archivo: " + e.Message);
        }
    }

    public void ForceReadNow()
    {
        readCount++;
        
        // Debug CADA lectura (no solo cada 100)
        Debug.Log($"[KEEPER FLAG] 🔄 LECTURA #{readCount} - Frame {Time.frameCount}");

        if (keeper == null)
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>();

        if (!File.Exists(filePath))
        {
            Apply(defaultValue, lastTimestamp, "missing");
            return;
        }

        string raw = "";
        try
        {
            raw = File.ReadAllText(filePath);
            string txt = raw.Trim();

            if (string.IsNullOrWhiteSpace(txt))
                return;

            // Formato: value,timestamp
            // Aceptamos separador coma o punto y coma
            string[] parts = txt.Split(',', ';');

            if (parts.Length < 2)
            {
                Debug.LogWarning("[KEEPER TS] Formato inválido (esperado value,ts): '" + txt + "'");
                return;
            }
        }

            int v;
            if (!int.TryParse(parts[0].Trim(), out v))
                v = 0;

            v = (v != 0) ? 1 : 0;

            long ts;
            if (!long.TryParse(parts[1].Trim(), out ts))
            {
                Debug.LogWarning("[KEEPER TS] Timestamp inválido: '" + parts[1] + "'");
                return;
            }

            // ✅ Solo aplicamos si el timestamp cambió
            if (ts == lastTimestamp)
                return;

            lastTimestamp = ts;

            Apply(v, ts, "read raw='" + raw + "'");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER TS] Error leyendo (raw='" + raw + "'): " + e.Message);
        }
    }

    void Apply(int v, long ts, string info)
    {
        CurrentAllowMove = (v == 1);

        if (keeper != null)
            keeper.SetExternalMoveAllowed(CurrentAllowMove);

        Debug.Log($"[KEEPER TS] v={v} allowMove={CurrentAllowMove} ts={ts} | {info}");
    }
}