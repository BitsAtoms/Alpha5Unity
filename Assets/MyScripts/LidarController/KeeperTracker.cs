using System;
using UnityEngine;

public class KeeperTracker : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target; 
    public Transform goalBottomLeft;
    public Transform goalTopRight;

    [Header("Sampling")]
    public float sampleHz = 60f;
    public float keepHistorySeconds = 2.0f;

    public struct Sample {
        public double ts;  
        public float lat01; 
        public float h01;   
    }

    // Buffer circular para evitar Garbage Collection y bloqueos de memoria
    private Sample[] _samples = new Sample[256]; 
    private int _head = 0;
    private int _tail = 0;
    private int _count = 0;
    private float _accum;

    void Awake() {
        if (target == null) target = transform; 
    }

    void Update() {
        if (target == null || goalBottomLeft == null || goalTopRight == null) return;

        _accum += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, sampleHz);

        while (_accum >= interval) {
            _accum -= interval;
            AddSample();
            PruneOld();
        }
    }

    void AddSample() {
        float lat01 = Mathf.InverseLerp(goalBottomLeft.position.z, goalTopRight.position.z, target.position.z);
        float h01 = Mathf.InverseLerp(goalBottomLeft.position.y, goalTopRight.position.y, target.position.y);

        double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        
        // Escribir en la cabeza del buffer circular (O(1))
        _samples[_head] = new Sample { ts = ts, lat01 = Mathf.Clamp01(lat01), h01 = Mathf.Clamp01(h01) };
        _head = (_head + 1) % _samples.Length;

        if (_count < _samples.Length) _count++;
        else _tail = (_tail + 1) % _samples.Length; // Sobrescribir el más viejo
    }

    void PruneOld() {
        double minTs = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - keepHistorySeconds;

        // Mover la cola en lugar de borrar datos de una lista (O(1) sin mover memoria)
        while (_count > 0 && _samples[_tail].ts < minTs) {
            _tail = (_tail + 1) % _samples.Length;
            _count--;
        }
    }

    public bool TryGetNearest(double ts, out Sample nearest) {
        nearest = default;
        if (_count == 0) return false;

        double bestDt = double.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < _count; i++) {
            int idx = (_tail + i) % _samples.Length;
            double d = Math.Abs(_samples[idx].ts - ts);
            if (d < bestDt) {
                bestDt = d;
                bestIndex = idx;
            }
        }

        if (bestIndex < 0) return false;
        nearest = _samples[bestIndex];
        return true;
    }
}