using System;
using UnityEngine;

public class LidarKeeperMatcher : MonoBehaviour
{
    public KeeperTracker keeperTracker;
    public GoalResultFlagReaderTimestamp goalReader;

    [Header("Tolerance")]
    public double maxTimeDiffSeconds = 0.12;

    [Header("Decision")]
    [Tooltip("Si la distancia (en espacio normalizado) es MAYOR o IGUAL que este valor => GOL")]
    public float goalDistanceThreshold = 0.25f;

    [Tooltip("Si TRUE, usa distancia 2D (lat+h). Si FALSE, usa solo lateral (lat).")]
    public bool use2DDistance = true;

    void OnEnable()
    {
        if (goalReader != null)
            goalReader.OnGoalSample += OnGoalSample;
    }

    void OnDisable()
    {
        if (goalReader != null)
            goalReader.OnGoalSample -= OnGoalSample;
    }

    void OnGoalSample(float lat01, float h01, double tsSeconds)
    {
        if (GameManager.I == null) return;

        if (keeperTracker == null)
        {
            Debug.LogWarning("[MATCH] keeperTracker NULL -> marco GOL por defecto");
            GameManager.I.GoalScored();
            return;
        }

        if (!keeperTracker.TryGetNearest(tsSeconds, out var k))
        {
            Debug.LogWarning("[MATCH] No hay muestras del portero -> marco GOL por defecto");
            GameManager.I.GoalScored();
            return;
        }

        double dt = Math.Abs(k.ts - tsSeconds);
        if (dt > maxTimeDiffSeconds)
        {
            Debug.LogWarning($"[MATCH] Timestamp fuera de tolerancia dt={dt*1000:F0}ms -> ignoro evento");
            return; // o decide gol/fallo; yo recomiendo ignorar
        }

        float dist;
        if (use2DDistance)
        {
            float dx = k.lat01 - lat01;
            float dy = k.h01 - h01;
            dist = Mathf.Sqrt(dx * dx + dy * dy);
        }
        else
        {
            dist = Mathf.Abs(k.lat01 - lat01); // solo lateral
        }

        Debug.Log($"[MATCH] LIDAR({lat01:F3},{h01:F3}) KEEPER({k.lat01:F3},{k.h01:F3}) dist={dist:F3}");

        // ✅ TU REGLA: GOL cuando la distancia es grande
        if (dist >= goalDistanceThreshold)
        {
            GameManager.I.GoalScored();
        }
        else
        {
            GameManager.I.ShotFail();
        }
    }
}
