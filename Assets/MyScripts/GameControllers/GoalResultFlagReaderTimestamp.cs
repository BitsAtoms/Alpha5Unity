using System.IO;
using UnityEngine;

public class GoalResultFlagReaderTimestamp : MonoBehaviour
{
    [Header("Archivo en ../Config")]
    public string fileName = "goal_result.txt";

    [Header("Lectura cada X segundos")]
    public float pollInterval = 0.08f;

    [Header("Solo aceptar eventos durante ronda jugable")]
    public bool onlyDuringPlayableRound = true;

    string filePath;
    long lastTimestamp = -1;

    void Awake()
    {
        filePath = Path.Combine(Application.dataPath, "../Config", fileName);
        Debug.Log("[GOAL TS] Ruta archivo = " + filePath);
        EnsureFileExists();
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
                Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
            {
                // vacío al inicio, no dispara nada
                File.WriteAllText(filePath, "");
                Debug.LogWarning("[GOAL TS] No existía -> creado vacío");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GOAL TS] Error asegurando archivo: " + e.Message);
        }
    }

    public void ForceReadNow()
    {
        if (onlyDuringPlayableRound && GameManager.I != null)
        {
            // Usa CanShoot si lo re-añadiste, o IsShotArmed si prefieres:
            // if (!GameManager.I.CanShoot()) return;

            if (!GameManager.I.CanShoot())
                return;
        }

        if (!File.Exists(filePath))
            return;

        string raw = "";
        try
        {
            raw = File.ReadAllText(filePath);
            string txt = raw.Trim();

            if (string.IsNullOrWhiteSpace(txt))
                return;

            string[] parts = txt.Split(',', ';');
            if (parts.Length < 2)
            {
                Debug.LogWarning("[GOAL TS] Formato inválido (esperado value,ts): '" + txt + "'");
                return;
            }

            int v;
            if (!int.TryParse(parts[0].Trim(), out v))
                v = 0;
            v = (v != 0) ? 1 : 0;

            long ts;
            if (!long.TryParse(parts[1].Trim(), out ts))
            {
                Debug.LogWarning("[GOAL TS] Timestamp inválido: '" + parts[1] + "'");
                return;
            }

            if (ts == lastTimestamp)
                return;

            lastTimestamp = ts;

            // ✅ Consumimos el evento para evitar repeticiones si el proceso externo deja el archivo igual
            // (opcional, pero yo lo recomiendo)
            File.WriteAllText(filePath, "");

            if (GameManager.I == null) return;

            if (v == 1)
            {
                Debug.Log($"[GOAL TS] GOL v=1 ts={ts}");
                GameManager.I.GoalScored();
            }
            else
            {
                Debug.Log($"[GOAL TS] FALLO v=0 ts={ts}");
                GameManager.I.ShotFail();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GOAL TS] Error leyendo (raw='" + raw + "'): " + e.Message);
        }
    }
}
