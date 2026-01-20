using System.IO;
using UnityEngine;

public class KeeperMoveFlagReader : MonoBehaviour
{
    [Header("Nombre del archivo (en carpeta ../Config)")]
    public string fileName = "keeper_move.txt";

    [Header("Frecuencia de lectura (segundos)")]
    public float pollInterval = 0.25f;

    [Header("Valor por defecto si falta/está vacío")]
    public int defaultValueIfMissingOrEmpty = 1; // 1 = se mueve, 0 = no se mueve

    private string filePath;
    private GoalkeeperAutoReact keeper;
    private int lastValue = -999;

    void Awake()
    {
        filePath = Path.Combine(Application.dataPath, "../Config", fileName);
        keeper = FindFirstObjectByType<GoalkeeperAutoReact>();

        Debug.Log("[KEEPER FLAG] Ruta archivo = " + filePath);
    }

    void OnEnable()
    {
        Debug.Log("[KEEPER FLAG] OnEnable() -> empieza a leer");
        lastValue = -999;

        EnsureFileExistsAndHasValue();

        ReadFlag();
        InvokeRepeating(nameof(ReadFlag), pollInterval, pollInterval);
    }

    void OnDisable()
    {
        Debug.Log("[KEEPER FLAG] OnDisable() -> deja de leer");
        CancelInvoke(nameof(ReadFlag));
    }

    // ✅ Para llamarlo justo al pulsar START (si quieres forzar lectura)
    public void ForceReadNow()
    {
        EnsureFileExistsAndHasValue();
        ReadFlag();
    }

    void EnsureFileExistsAndHasValue()
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, defaultValueIfMissingOrEmpty.ToString());
                Debug.LogWarning("[KEEPER FLAG] No existía -> creado con valor " + defaultValueIfMissingOrEmpty);
                return;
            }

            string raw = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                File.WriteAllText(filePath, defaultValueIfMissingOrEmpty.ToString());
                Debug.LogWarning("[KEEPER FLAG] Archivo vacío -> rellenado con valor " + defaultValueIfMissingOrEmpty);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER FLAG] Error asegurando archivo: " + e.Message);
        }
    }

    void ReadFlag()
    {
        if (keeper == null)
        {
            keeper = FindFirstObjectByType<GoalkeeperAutoReact>();
            if (keeper == null) return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("[KEEPER FLAG] No existe archivo -> aplico default " + defaultValueIfMissingOrEmpty);
            ApplyValue(defaultValueIfMissingOrEmpty, "missing");
            return;
        }

        string raw = "";
        try
        {
            raw = File.ReadAllText(filePath);
            string txt = raw.Trim();

            if (string.IsNullOrWhiteSpace(txt))
            {
                Debug.LogWarning("[KEEPER FLAG] Contenido vacío -> aplico default " + defaultValueIfMissingOrEmpty);
                ApplyValue(defaultValueIfMissingOrEmpty, "empty");
                return;
            }

            int value;
            if (!int.TryParse(txt, out value))
            {
                Debug.LogWarning("[KEEPER FLAG] No pude parsear: '" + txt + "' (raw='" + raw + "') -> lo tomo como 0");
                value = 0;
            }

            value = (value != 0) ? 1 : 0; // normalizar a 0/1

            ApplyValue(value, "read raw='" + raw + "'");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[KEEPER FLAG] Error leyendo archivo (raw='" + raw + "'): " + e.Message);
        }
    }

    void ApplyValue(int value, string info)
    {
        if (value != lastValue)
        {
            lastValue = value;
            bool allow = (value == 1);
            keeper.SetExternalMoveAllowed(allow);
            Debug.Log("[KEEPER FLAG] " + value + " -> allowMove=" + allow + " | " + info);
        }
    }
}
