using System.IO;
using UnityEngine;

public class KeeperMoveFlagReader : MonoBehaviour
{
    [Header("Nombre del archivo (en carpeta Config)")]
    public string fileName = "keeper_move.txt";

    [Header("Frecuencia de lectura (segundos)")]
    public float pollInterval = 0.1f;

    [Header("Ruta: Config al lado de Assets (recomendado)")]
    public bool useProjectConfigFolder = true;

    private string filePath;
    private GoalkeeperAutoReact keeper;

    private int lastValue = -999;
    private bool firedWhile1 = false;
    private int readCount = 0;

    void Awake()
    {
        Debug.Log("[KEEPER FLAG] ==================== AWAKE ====================");
        
        keeper = FindFirstObjectByType<GoalkeeperAutoReact>();

        if (keeper == null)
        {
            Debug.LogError("[KEEPER FLAG] ❌❌❌ NO ENCONTRÉ GoalkeeperAutoReact!");
        }
        else
        {
            Debug.Log("[KEEPER FLAG] ✅ GoalkeeperAutoReact encontrado: " + keeper.gameObject.name);
        }

        if (useProjectConfigFolder)
        {
            filePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Config", fileName));
        }
        else
        {
            filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        Debug.Log("[KEEPER FLAG] 📁 RUTA COMPLETA DEL ARCHIVO:");
        Debug.Log("[KEEPER FLAG]    " + filePath);
        Debug.Log("[KEEPER FLAG] 📁 ¿Existe? " + File.Exists(filePath));
    }

    void OnEnable()
    {
        Debug.Log("[KEEPER FLAG] ==================== OnEnable ====================");
        EnsureFileExists("0");
        
        // Cancelar cualquier invoke anterior
        CancelInvoke(nameof(ReadFlag));
        
        // Iniciar nuevo invoke
        InvokeRepeating(nameof(ReadFlag), 0f, pollInterval);
        
        Debug.Log("[KEEPER FLAG] ✅ InvokeRepeating ACTIVADO cada " + pollInterval + " seg");
    }

    void OnDisable()
    {
        Debug.Log("[KEEPER FLAG] ==================== OnDisable ====================");
        CancelInvoke(nameof(ReadFlag));
    }

    void EnsureFileExists(string defaultValue)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, defaultValue);
                Debug.LogWarning("[KEEPER FLAG] ⚠️ Archivo creado con: '" + defaultValue + "'");
            }
            else
            {
                string currentContent = File.ReadAllText(filePath);
                Debug.Log("[KEEPER FLAG] ✅ Archivo existe. Contenido actual: '" + currentContent + "'");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[KEEPER FLAG] ❌ Error con archivo: " + e.Message);
        }
    }

    void ReadFlag()
    {
        if (keeper == null)
        {
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
            if (keeper == null)
            {
                Debug.LogError("[KEEPER FLAG] ❌ NO encuentro GoalkeeperAutoReact!");
                return;
            }
        }

        string raw;
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
            Debug.LogError("[KEEPER FLAG] ❌ Error leyendo: " + e.Message);
            return;
        }

        int value = (raw.Length > 0 && raw[0] == '1') ? 1 : 0;
        Debug.Log($"[KEEPER FLAG]    Valor interpretado: {value}");

        // SIEMPRE log el cambio
        if (value != lastValue)
        {
            Debug.Log($"[KEEPER FLAG] 🔔🔔🔔 CAMBIO DETECTADO: {lastValue} -> {value}");
            lastValue = value;
        }

        // Aplicar bloqueo/permiso
        keeper.SetExternalMoveAllowed(value == 1);

        // Disparar cuando sea 1
        if (value == 1 && !firedWhile1)
        {
            firedWhile1 = true;
            Debug.Log("[KEEPER FLAG] 🚀🚀🚀 DISPARO ANIMACIÓN 🚀🚀🚀");
            keeper.TriggerRandomDiveThisRound_NoShotWindow();
        }
        else if (value == 0)
        {
            if (firedWhile1)
            {
                Debug.Log("[KEEPER FLAG] 🔄 REARM - Listo para próximo 1");
            }
            firedWhile1 = false;
            keeper.RearmFromExternalTrigger();
        }
    }

    // Para probar manualmente desde Inspector
    [ContextMenu("Leer Archivo AHORA")]
    public void ForceReadNow()
    {
        Debug.Log("[KEEPER FLAG] ========== LECTURA FORZADA MANUAL ==========");
        ReadFlag();
    }

    [ContextMenu("Mostrar Info")]
    public void ShowInfo()
    {
        Debug.Log("[KEEPER FLAG] ========== INFO ==========");
        Debug.Log("[KEEPER FLAG] Ruta: " + filePath);
        Debug.Log("[KEEPER FLAG] Existe: " + File.Exists(filePath));
        Debug.Log("[KEEPER FLAG] Lecturas realizadas: " + readCount);
        Debug.Log("[KEEPER FLAG] Último valor: " + lastValue);
        Debug.Log("[KEEPER FLAG] firedWhile1: " + firedWhile1);
        Debug.Log("[KEEPER FLAG] InvokeRepeating activo: " + IsInvoking(nameof(ReadFlag)));
        
        if (File.Exists(filePath))
        {
            Debug.Log("[KEEPER FLAG] Contenido actual: '" + File.ReadAllText(filePath) + "'");
        }
    }
}
