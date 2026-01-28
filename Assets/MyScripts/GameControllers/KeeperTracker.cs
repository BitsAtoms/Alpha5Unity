using System;
using System.Collections.Generic;
using UnityEngine;

public class KeeperTracker : MonoBehaviour
{
    [Header("What to track (usually the base/root goalkeeper Transform)")]
    public Transform target; // <- arrastra aquí el 'portero' (base) del GoalkeeperAutoReact

    [Header("Goal area reference (2x2 equivalent)")]
    public Transform goalBottomLeft;
    public Transform goalTopRight;

    [Header("Sampling")]
    public float sampleHz = 60f;
    public float keepHistorySeconds = 2.0f;

    public struct Sample
    {
        public double ts;   // epoch seconds
        public float lat01; // 0..1 (lateral) => Z
        public float h01;   // 0..1 (height)  => Y
    }

    private readonly List<Sample> _samples = new List<Sample>(512);
    private float _accum;

    void Awake()
    {
        if (target == null) target = transform; // fallback
    }

    void Update()
    {
        if (target == null || goalBottomLeft == null || goalTopRight == null) return;

        _accum += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, sampleHz);

        while (_accum >= interval)
        {
            _accum -= interval;
            AddSample();
            PruneOld();
        }
    }

    void AddSample()
    {
        // ✅ lee SIEMPRE del target (base del portero)
        Vector3 p = target.position;

        float lateralZ = p.z;
        float heightY = p.y;

        float leftZ = goalBottomLeft.position.z;
        float rightZ = goalTopRight.position.z;

        float bottomY = goalBottomLeft.position.y;
        float topY = goalTopRight.position.y;

        float lat01 = Mathf.InverseLerp(leftZ, rightZ, lateralZ);
        float h01 = Mathf.InverseLerp(bottomY, topY, heightY);

        lat01 = Mathf.Clamp01(lat01);
        h01 = Mathf.Clamp01(h01);

        double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        _samples.Add(new Sample { ts = ts, lat01 = lat01, h01 = h01 });
    }

    void PruneOld()
    {
        double now = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        double minTs = now - keepHistorySeconds;

        int removeCount = 0;
        for (int i = 0; i < _samples.Count; i++)
        {
            if (_samples[i].ts < minTs) removeCount++;
            else break;
        }
        if (removeCount > 0) _samples.RemoveRange(0, removeCount);
    }

    public bool TryGetNearest(double ts, out Sample nearest)
    {
        nearest = default;
        if (_samples.Count == 0) return false;

        double bestDt = double.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < _samples.Count; i++)
        {
            double d = Math.Abs(_samples[i].ts - ts);
            if (d < bestDt)
            {
                bestDt = d;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return false;
        nearest = _samples[bestIndex];
        return true;
    }
}
