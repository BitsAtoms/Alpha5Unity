using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LidarEventDetector : MonoBehaviour
{
    [Header("Configuración RPLIDAR")]
    public string comPort = "COM7"; 
    public int baudrate = 460800;

    [Header("Área de Detección (Metros)")]
    public float latMaxM = 2.0f;
    public float heightMaxM = 2.0f;
    public float angleOffsetDeg = 270f;
    public float angleMin = 0f;
    public float angleMax = 90f;

    [Header("Parámetros de Detección")]
    public float deltaMm = 150f;         
    public int minPointsEvent = 3;       
    public float eventCooldownS = 0.5f; 
    public float baselineSeconds = 2.0f; 

    public event Action<float, float, double> OnGoalDetected;

    private RplidarNative.ScanPoint[] _buf = new RplidarNative.ScanPoint[8192];
    private float[] _baseline = new float[360];
    private bool[] _hasBaseline = new bool[360];
    private List<float>[] _baselineCollect = new List<float>[360];
    
    private float[] _sinCache = new float[360];
    private float[] _cosCache = new float[360];

    private bool _baselineReady = false;
    private DateTime _baselineStartTime;
    private DateTime _lastEventTime = DateTime.MinValue;

    private Thread _lidarThread;
    private volatile bool _isRunning = false;
    
    // ✅ NUEVO: Interruptor volátil para el hilo secundario
    private volatile bool _isDetectionArmed = false; 

    void Awake()
    {
        for (int i = 0; i < 360; i++) {
            _baselineCollect[i] = new List<float>(32);
            float rad = i * Mathf.Deg2Rad;
            _sinCache[i] = Mathf.Sin(rad);
            _cosCache[i] = Mathf.Cos(rad);
        }
    }

    void OnEnable() {
        int ok = RplidarNative.rl_connect(comPort, baudrate);
        Debug.Log($"[LIDAR] Conectando: {ok} en {comPort}");
        RplidarNative.rl_start_scan();

        _baselineStartTime = DateTime.UtcNow;
        _isRunning = true;
        
        _lidarThread = new Thread(LidarLoop) { IsBackground = true };
        _lidarThread.Start();
    }

    void OnDisable() {
        _isRunning = false;
        if (_lidarThread != null) _lidarThread.Join(500);
        RplidarNative.rl_stop();
        RplidarNative.rl_disconnect();
    }

    // ✅ NUEVO: Conectamos el estado del juego con el hilo del LiDAR
    void Update() 
    {
        if (GameManager.I != null) 
        {
            // El LiDAR solo se "arma" cuando GameManager.CanShoot() es TRUE
            // (Es decir, JUSTO después de que el sensor detecte la pelota y hasta que haya gol/fallo)
            _isDetectionArmed = GameManager.I.CanShoot();
        }
    }

    void LidarLoop() {
        while (_isRunning) {
            int n = RplidarNative.rl_grab_points(_buf, _buf.Length);
            
            if (n > 0) {
                if (!_baselineReady) {
                    BuildBaseline(n);
                } 
                // ✅ LA CLAVE DEL RENDIMIENTO: Solo procesa los miles de puntos si el tiro está en curso
                else if (_isDetectionArmed) {
                    ProcessPointsFast(n);
                }
                // Si _isDetectionArmed es false, no hace NADA. Los puntos se descartan y la CPU descansa.
            } else {
                Thread.Sleep(5); 
            }
        }
    }

    void BuildBaseline(int n) {
        for (int i = 0; i < n; i++) {
            var p = _buf[i];
            if (p.distMm <= 0) continue;
            
            float aCorr = (p.angleDeg - angleOffsetDeg) % 360f;
            if (aCorr < 0) aCorr += 360f;
            
            if (aCorr < angleMin || aCorr > angleMax) continue;

            int bin = (int)aCorr;
            _baselineCollect[bin].Add(p.distMm);
        }

        if ((DateTime.UtcNow - _baselineStartTime).TotalSeconds >= baselineSeconds) {
            for (int i = 0; i < 360; i++) {
                if (_baselineCollect[i].Count > 0) {
                    _baselineCollect[i].Sort();
                    _baseline[i] = _baselineCollect[i][_baselineCollect[i].Count / 2];
                    _hasBaseline[i] = true;
                }
            }
            _baselineReady = true;

            UnityMainThreadDispatcher.Enqueue(() => {
                Debug.Log("⚡ [LIDAR] Fondo memorizado. LiDAR en reposo esperando al sensor...");
            });
        }
    }

    void ProcessPointsFast(int n) {
        if ((DateTime.UtcNow - _lastEventTime).TotalSeconds < eventCooldownS) return;

        int count = 0;
        float sumLat = 0, sumH = 0;
        
        for (int i = 0; i < n; i++) {
            var p = _buf[i];
            if (p.distMm <= 0) continue;
            
            float aCorr = (p.angleDeg - angleOffsetDeg) % 360f;
            if (aCorr < 0) aCorr += 360f;
            
            if (aCorr < angleMin || aCorr > angleMax) continue;
            
            int bin = (int)aCorr;
            if (!_hasBaseline[bin]) continue;
            
            if (_baseline[bin] - p.distMm > deltaMm) {
                float distM = p.distMm * 0.001f; 
                float latM = distM * _sinCache[bin];
                float hM = distM * _cosCache[bin];
                
                if (latM >= 0 && latM <= latMaxM && hM >= 0 && hM <= heightMaxM) {
                    count++;
                    sumLat += latM; 
                    sumH += hM;
                }
            }
        }

        if (count >= minPointsEvent) {
            float lat01 = (sumLat / count) / latMaxM;
            float h01 = (sumH / count) / heightMaxM;
            double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            
            _lastEventTime = DateTime.UtcNow; 
            
            UnityMainThreadDispatcher.Enqueue(() => {
                OnGoalDetected?.Invoke(Mathf.Clamp01(lat01), Mathf.Clamp01(h01), ts);
            });
        }
    }
}