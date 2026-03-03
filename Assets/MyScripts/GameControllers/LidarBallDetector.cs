using System;
using System.Collections.Generic;
using UnityEngine;

public class LidarBallDetector : MonoBehaviour
{
    [Header("Configuración RPLIDAR")]
    public string comPort = "COM8";
    public int baudrate = 460800;

    [Header("Área de Detección (Metros)")]
    public float latMaxM = 2.0f;
    public float heightMaxM = 2.0f;

    [Header("Filtros de Ángulo (Sector)")]
    public float angleOffsetDeg = 270f;
    public float angleMin = 0f;
    public float angleMax = 90f;

    [Header("Parámetros de Detección")]
    public float deltaMm = 150f;         // Diferencia mínima con el fondo para detectar objeto
    public int minPointsEvent = 3;       // Puntos mínimos para considerar un impacto
    public float eventCooldownS = 0.5f;  // Tiempo de espera entre detecciones
    public float baselineSeconds = 2.0f; // Tiempo para calcular el fondo inicial

    // Evento que escuchará el LidarKeeperMatcher
    public event Action<float, float, double> OnGoalDetected;

    private RplidarNative.ScanPoint[] _buf = new RplidarNative.ScanPoint[8192];
    private Dictionary<int, float> _baseline = new Dictionary<int, float>();
    private Dictionary<int, List<float>> _baselineCollect = new Dictionary<int, List<float>>();
    
    private bool _baselineReady = false;
    private float _baselineTimer = 0f;
    private float _lastEventTime = -999f;

    void OnEnable()
    {
        int ok = RplidarNative.rl_connect(comPort, baudrate);
        Debug.Log($"[LIDAR] Conectando: {ok} en {comPort}");
        RplidarNative.rl_start_scan();
        ResetBaseline();
    }

    void OnDisable()
    {
        RplidarNative.rl_stop();
        RplidarNative.rl_disconnect();
    }

    void ResetBaseline()
    {
        _baselineReady = false;
        _baselineTimer = 0f;
        _baselineCollect.Clear();
        _baseline.Clear();
    }

    void Update()
    {
        int n = RplidarNative.rl_grab_points(_buf, _buf.Length);
        if (n <= 0) return;

        if (!_baselineReady)
        {
            BuildBaseline(n);
            return;
        }

        ProcessPoints(n);
    }

    void BuildBaseline(int n)
    {
        _baselineTimer += Time.unscaledDeltaTime;

        for (int i = 0; i < n; i++)
        {
            var p = _buf[i];
            if (p.distMm <= 0) continue;

            float aCorr = WrapDeg(p.angleDeg - angleOffsetDeg);
            if (!AngleInSector(aCorr)) continue;

            int bin = (int)aCorr;
            if (!_baselineCollect.ContainsKey(bin)) _baselineCollect[bin] = new List<float>();
            _baselineCollect[bin].Add(p.distMm);
        }

        if (_baselineTimer >= baselineSeconds)
        {
            foreach (var kv in _baselineCollect)
            {
                kv.Value.Sort();
                _baseline[kv.Key] = kv.Value[kv.Value.Count / 2]; // Mediana
            }
            _baselineReady = true;
            Debug.Log("[LIDAR] Fondo (Baseline) calculado correctamente.");
        }
    }

    void ProcessPoints(int n)
    {
        if (Time.unscaledTime - _lastEventTime < eventCooldownS) return;

        int candidateCount = 0;
        float sumLat = 0, sumH = 0, minDist = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            var p = _buf[i];
            if (p.distMm <= 0) continue;

            float aCorr = WrapDeg(p.angleDeg - angleOffsetDeg);
            if (!AngleInSector(aCorr)) continue;

            int bin = (int)aCorr;
            if (!_baseline.TryGetValue(bin, out float baseD)) continue;

            if (baseD - p.distMm > deltaMm)
            {
                float distM = p.distMm / 1000f;
                float angRad = aCorr * Mathf.Deg2Rad;
                float latM = distM * Mathf.Sin(angRad);
                float hM = distM * Mathf.Cos(angRad);

                if (latM >= 0 && latM <= latMaxM && hM >= 0 && hM <= heightMaxM)
                {
                    candidateCount++;
                    sumLat += latM;
                    sumH += hM;
                    if (p.distMm < minDist) minDist = p.distMm;
                }
            }
        }

        if (candidateCount >= minPointsEvent)
        {
            float avgLat01 = (sumLat / candidateCount) / latMaxM;
            float avgH01 = (sumH / candidateCount) / heightMaxM;
            double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            _lastEventTime = Time.unscaledTime;
            
            // ✅ Lanza el evento directo al Matcher
            OnGoalDetected?.Invoke(Mathf.Clamp01(avgLat01), Mathf.Clamp01(avgH01), ts);
            Debug.Log($"[LIDAR] IMPACTO DETECTADO: Lat={avgLat01:F2} H={avgH01:F2}");
        }
    }

    float WrapDeg(float a) => (a % 360f + 360f) % 360f;
    bool AngleInSector(float a) => a >= angleMin && a <= angleMax;
}