using System;
using UnityEngine;

public class LidarKeeperMatcher : MonoBehaviour
{
    [Header("Referencias")]
    public KeeperTracker keeperTracker;
    
    // ✅ Cambiamos la referencia al nuevo detector que procesa la nube de puntos
    // Si aún no lo has creado, este sería el script que usa RplidarNative
    public LidarBallDetector lidarDetector; 

    [Header("Tolerance")]
    public double maxTimeDiffSeconds = 0.12;

    [Header("Decision")]
    [Tooltip("Si la distancia es MAYOR o IGUAL que este valor => GOL")]
    public float goalDistanceThreshold = 0.25f;

    [Tooltip("Si TRUE, usa distancia 2D (lat+h). Si FALSE, usa solo lateral (lat).")]
    public bool use2DDistance = true;

    // ✅ Suscripción al evento de detección
    void OnEnable()
    {
        if (lidarDetector != null)
            lidarDetector.OnGoalDetected += OnGoalSample;
    }

    void OnDisable()
    {
        if (lidarDetector != null)
            lidarDetector.OnGoalDetected -= OnGoalSample;
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

        // Obtener la posición del portero en el momento exacto del impacto
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
            return; 
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
            dist = Mathf.Abs(k.lat01 - lat01);
        }

        Debug.Log($"[MATCH] LIDAR({lat01:F3},{h01:F3}) KEEPER({k.lat01:F3},{k.h01:F3}) dist={dist:F3}");

        // Regla: GOL si la distancia es lo suficientemente grande (el portero no llegó)
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