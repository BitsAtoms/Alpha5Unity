using System;
using UnityEngine;

public class LidarMatchToGameManagerBridge : MonoBehaviour
{
    [Header("References")]
    public GoalResultFlagReaderTimestamp goalReader;
    public KeeperTracker keeperTracker;

    [Header("Tolerance")]
    public double maxTimeDiffSeconds = 0.12;
    public float maxSpatialDistance01 = 0.12f;

    [Header("Rule")]
    [Tooltip("true = GOL si el portero está lejos del punto de impacto (distancia grande)")]
    public bool goalWhenDistanceIsLarge = true;

    [Header("Debug")]
    public bool logDecision = true;

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
        if (GameManager.I == null)
        {
            Debug.LogWarning("[BRIDGE] GameManager.I null");
            return;
        }

        if (keeperTracker == null)
        {
            Debug.LogWarning("[BRIDGE] keeperTracker null -> FAIL");
            GameManager.I.ShotFail();
            return;
        }

        if (!keeperTracker.TryGetNearest(tsSeconds, out var k))
        {
            if (logDecision) Debug.Log("[BRIDGE] No keeper sample -> FAIL");
            GameManager.I.ShotFail();
            return;
        }

        double dt = Math.Abs(k.ts - tsSeconds);

        float dx = k.lat01 - lat01;
        float dy = k.h01 - h01;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        if (logDecision)
        {
            Debug.Log($"[BRIDGE] LIDAR({lat01:F3},{h01:F3}) KEEPER({k.lat01:F3},{k.h01:F3}) dt={dt*1000.0:F0}ms dist={dist:F3}");
        }

        if (dt > maxTimeDiffSeconds)
        {
            if (logDecision) Debug.Log("[BRIDGE] dt too big -> FAIL");
            GameManager.I.ShotFail();
            return;
        }

        bool keeperClose = dist <= maxSpatialDistance01;

        // ✅ TU REGLA: GOL cuando la distancia es grande
        bool goal = goalWhenDistanceIsLarge ? !keeperClose : keeperClose;

        if (goal)
        {
            if (logDecision) Debug.Log("[BRIDGE] => GOAL");
            GameManager.I.GoalScored();
        }
        else
        {
            if (logDecision) Debug.Log("[BRIDGE] => FAIL");
            GameManager.I.ShotFail();
        }
    }
}
