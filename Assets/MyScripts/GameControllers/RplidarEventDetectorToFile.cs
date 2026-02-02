using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RplidarEventDetectorToFile : MonoBehaviour
{
    [Header("RPLIDAR C1")]
    public string comPort = "COM3";
    public int baudrate = 460800;

    [Header("Output")]
    public string fileName = "goal_result.txt";

    [Header("Area 2x2 (meters)")]
    public float latMaxM = 2.0f;
    public float heightMaxM = 2.0f;

    [Header("Sector")]
    public float angleOffsetDeg = 270f;
    public float angleMin = 0f;
    public float angleMax = 90f;

    [Header("Basic Filters")]
    public float minDistMm = 30f;
    public float maxDistMm = 8000f;
    public int minQuality = 0;

    [Header("Baseline / Detection")]
    public float baselineSeconds = 2.0f;
    public int angleBinDeg = 2;          // como Python (ANGLE_BIN_DEG)
    public float deltaMm = 150f;         // como Python (DELTA_MM)
    public int minPointsEvent = 3;       // como Python (MIN_POINTS_EVENT)
    public float eventCooldownS = 0.3f;  // como Python (EVENT_COOLDOWN_S)
    [Range(0f, 0.999f)]
    public float baselineAlpha = 0.97f;  // como Python (BASELINE_ALPHA)

    [Header("Debug")]
    public bool logBaselineReady = true;
    public bool logEvents = true;

    string _outPath;

    // buffer de puntos
    private RplidarNative.ScanPoint[] _buf = new RplidarNative.ScanPoint[8192];

    // baseline:
    // - mientras construimos baseline: guardamos listas por bin para mediana
    private readonly Dictionary<int, List<float>> _baselineCollect = new Dictionary<int, List<float>>(256);
    // - baseline final: mediana por bin (mm)
    private readonly Dictionary<int, float> _baseline = new Dictionary<int, float>(256);

    private float _baselineTimer = 0f;
    private bool _baselineReady = false;

    private float _lastEventTime = -999f;

    struct Candidate
    {
        public float distMm;
        public float latM;
        public float hM;
    }

    void Awake()
    {
        _outPath = Path.Combine(Application.dataPath, "../Config", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(_outPath));
    }

    void OnEnable()
    {
        int ok = RplidarNative.rl_connect(comPort, baudrate);
        Debug.Log($"[RPLIDAR] connect={ok} port={comPort} baud={baudrate}");

        ok = RplidarNative.rl_start_scan();
        Debug.Log($"[RPLIDAR] start_scan={ok}");

        // reset baseline
        _baselineCollect.Clear();
        _baseline.Clear();
        _baselineTimer = 0f;
        _baselineReady = false;

        _lastEventTime = -999f;
    }

    void OnDisable()
    {
        try { RplidarNative.rl_stop(); } catch { }
        try { RplidarNative.rl_disconnect(); } catch { }
    }

    void Update()
    {
        int n = RplidarNative.rl_grab_points(_buf, _buf.Length);
        if (n <= 0) return;

        if (!_baselineReady)
        {
            BuildBaselineStep(n);
            return;
        }

        DetectEventStep(n);
    }

    // ---------------- BASELINE ----------------
    void BuildBaselineStep(int n)
    {
        _baselineTimer += Time.unscaledDeltaTime;

        for (int i = 0; i < n; i++)
        {
            var p = _buf[i];

            if (p.distMm <= 0 || p.distMm < minDistMm || p.distMm > maxDistMm) continue;
            if (p.quality < minQuality) continue;

            float aCorr = AngleCorrected(p.angleDeg);
            if (!AngleInSector(aCorr)) continue;

            int b = Bin(aCorr);
            if (!_baselineCollect.TryGetValue(b, out var list))
            {
                list = new List<float>(64);
                _baselineCollect[b] = list;
            }
            list.Add(p.distMm);
        }

        if (_baselineTimer < baselineSeconds) return;

        // construir baseline por mediana
        _baseline.Clear();
        foreach (var kv in _baselineCollect)
        {
            var list = kv.Value;
            if (list == null || list.Count == 0) continue;

            list.Sort();
            float med = list[list.Count / 2];
            _baseline[kv.Key] = med;
        }

        _baselineReady = _baseline.Count > 0;

        if (logBaselineReady)
            Debug.Log($"[RPLIDAR] Baseline ready={_baselineReady} bins={_baseline.Count}");

        // si por algún motivo no hay bins, reinicia
        if (!_baselineReady)
        {
            _baselineCollect.Clear();
            _baselineTimer = 0f;
        }
    }

    // ---------------- DETECTION ----------------
    void DetectEventStep(int n)
    {
        // cooldown
        if (Time.unscaledTime - _lastEventTime < eventCooldownS) return;

        List<Candidate> candidates = null;
        int candidateCount = 0;
        Candidate best = default;
        bool hasBest = false;

        for (int i = 0; i < n; i++)
        {
            var p = _buf[i];

            if (p.distMm <= 0 || p.distMm < minDistMm || p.distMm > maxDistMm) continue;
            if (p.quality < minQuality) continue;

            float aCorr = AngleCorrected(p.angleDeg);
            if (!AngleInSector(aCorr)) continue;

            int b = Bin(aCorr);
            if (!_baseline.TryGetValue(b, out float baseD)) continue;

            float diff = baseD - p.distMm;
            if (diff < deltaMm) continue;

            // coordenadas como Python:
            // lateral = dist * sin(angle)
            // altura  = dist * cos(angle)
            float distM = p.distMm / 1000f;
            float angRad = aCorr * Mathf.Deg2Rad;

            float latM = distM * Mathf.Sin(angRad);
            float hM = distM * Mathf.Cos(angRad);

            if (latM < 0 || hM < 0) continue;
            if (latM > latMaxM || hM > heightMaxM) continue;

            candidateCount++;

            // escoger el más cercano (menor distMm) como en tu Python
            if (!hasBest || p.distMm < best.distMm)
            {
                best = new Candidate { distMm = p.distMm, latM = latM, hM = hM };
                hasBest = true;
            }

            // baseline adaptativo (igual idea que Python)
            _baseline[b] = baselineAlpha * _baseline[b] + (1f - baselineAlpha) * p.distMm;
        }

        if (candidateCount < minPointsEvent) return;
        if (!hasBest) return;

        float lat01 = Mathf.Clamp01(best.latM / latMaxM);
        float h01 = Mathf.Clamp01(best.hM / heightMaxM);
        long tsMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        File.WriteAllText(_outPath, $"{lat01:0.0000},{h01:0.0000},{tsMs}\n");

        _lastEventTime = Time.unscaledTime;

        if (logEvents)
            Debug.Log($"[RPLIDAR] EVENT lat01={lat01:0.0000} h01={h01:0.0000} tsMs={tsMs} cand={candidateCount}");
    }

    // ---------------- HELPERS ----------------
    float WrapDeg(float a) => (a % 360f + 360f) % 360f;

    float AngleCorrected(float rawDeg) => WrapDeg(rawDeg - angleOffsetDeg);

    bool AngleInSector(float aCorrDeg)
    {
        float a = WrapDeg(aCorrDeg);
        float amin = WrapDeg(angleMin);
        float amax = WrapDeg(angleMax);
        if (amin <= amax) return a >= amin && a <= amax;
        return a >= amin || a <= amax;
    }

    int Bin(float aCorrDeg)
    {
        int binSize = Mathf.Max(1, angleBinDeg);
        return (int)(WrapDeg(aCorrDeg) / binSize);
    }
}
