using System.IO;
using UnityEngine;

public class GoalResultFlagReaderTimestamp : MonoBehaviour
{
    public string fileName = "goal_result.txt";
    public float pollInterval = 0.08f;

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
        if (parts.Length < 2) return;

        if (!int.TryParse(parts[0], out int v)) v = 0;
        if (!long.TryParse(parts[1], out long ts)) return;

        if (ts == lastTimestamp) return;
        lastTimestamp = ts;

        File.WriteAllText(filePath, ""); // consumir evento

        if (v == 1)
        {
            Debug.Log($"[GOAL TS] GOL ts={ts}");
            GameManager.I.GoalScored();
        }
        else
        {
            Debug.Log($"[GOAL TS] FALLO ts={ts}");
            GameManager.I.ShotFail();
        }
    }
}
