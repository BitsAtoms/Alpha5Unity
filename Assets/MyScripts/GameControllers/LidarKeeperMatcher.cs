using System;
using UnityEngine;

public class LidarKeeperMatcher : MonoBehaviour
{
    [Header("Referencias")]
    public KeeperTracker keeperTracker;
    public LidarEventDetector lidarDetector; 

    [Header("Tolerance")]
    public double maxTimeDiffSeconds = 0.12;
    public float goalDistanceThreshold = 0.25f;
    public bool use2DDistance = true;

    void OnEnable() {
        if (lidarDetector) lidarDetector.OnGoalDetected += OnGoalSample;
    }

    void OnDisable() {
        if (lidarDetector) lidarDetector.OnGoalDetected -= OnGoalSample;
    }

    void OnGoalSample(float lat01, float h01, double tsSeconds) {
        if (GameManager.I == null) return;

        // ✅ LA CLAVE: Si el juego no está listo para un tiro, ignoramos el LiDAR por completo
        if (!GameManager.I.CanShoot()) {
            return; // No hace nada durante reinicios o pausas
        }

        if (keeperTracker == null || !keeperTracker.TryGetNearest(tsSeconds, out var k)) {
            GameManager.I.GoalScored();
            return;
        }

        double dt = Math.Abs(k.ts - tsSeconds);
        if (dt > maxTimeDiffSeconds) return; 

        float dist = use2DDistance 
            ? Mathf.Sqrt(Mathf.Pow(k.lat01 - lat01, 2) + Mathf.Pow(k.h01 - h01, 2)) 
            : Mathf.Abs(k.lat01 - lat01);

        Debug.Log($"[MATCH] Distancia Impacto-Portero: {dist:F3}");

        if (dist >= goalDistanceThreshold) GameManager.I.GoalScored();
        else GameManager.I.ShotFail();
    }
}