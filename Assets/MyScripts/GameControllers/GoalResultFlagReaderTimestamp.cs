using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class GoalResultFlagReaderTimestamp : MonoBehaviour
{
    public string fileName = "goal_result.txt";
    public float pollInterval = 0.08f;

    string filePath;
    long lastTimestampMs = -1;

    public event Action<float, float, double> OnGoalSample; 
    // lat01, h01, tsSeconds

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
        string dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(filePath))
            File.WriteAllText(filePath, "");
    }

    public void ForceReadNow()
    {
        if (GameManager.I == null || !GameManager.I.CanShoot())
            return;

        string txt = File.ReadAllText(filePath).Trim();
        if (string.IsNullOrWhiteSpace(txt)) return;

        string[] parts = txt.Split(',', ';');
        if (parts.Length < 3) return;

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat01)) return;
        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float h01)) return;
        if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long tsMs)) return;

        if (tsMs == lastTimestampMs) return;
        lastTimestampMs = tsMs;

        // Consumir evento
        File.WriteAllText(filePath, "");

        lat01 = Mathf.Clamp01(lat01);
        h01 = Mathf.Clamp01(h01);

        double tsSeconds = tsMs / 1000.0;

        Debug.Log($"[GOAL TS] Evento LIDAR lat={lat01:F3} h={h01:F3} tsMs={tsMs}");

        OnGoalSample?.Invoke(lat01, h01, tsSeconds);
    }
}
