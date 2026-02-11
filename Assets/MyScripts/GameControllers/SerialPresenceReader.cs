using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialPresenceReader : MonoBehaviour
{
    [Header("Configuración Serial")]
    public string portName = "COM4";
    public int baudRate = 115200;

    [Header("Lógica de Pulso")]
    public float inactiveTimeout = 0.6f;
    public string presenceFileName = "keeper_move.txt";

    private SerialPort _sp;
    private Thread _thread;
    private volatile bool _run;
    private string _presencePath;

    private bool _hasPulse = false;
    private bool _triggered = false;
    private long _lastValidTs = 0; 
    private readonly object _lock = new object();

    void Awake()
    {
        _presencePath = Path.Combine(Application.dataPath, "../Config", presenceFileName);
        string dir = Path.GetDirectoryName(_presencePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    void OnEnable() => StartPort();
    void OnDisable() => StopPort();

    void StartPort()
    {
        try {
            _sp = new SerialPort(portName, baudRate) { ReadTimeout = 200 };
            _sp.Open();
            _run = true;
            _thread = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();
            Debug.Log($"[SERIAL] Puerto {portName} abierto.");
        } catch (Exception e) { Debug.LogError($"[SERIAL] Error: {e.Message}"); }
    }

    void StopPort()
    {
        _run = false;
        if (_thread != null) _thread.Join(500);
        if (_sp != null && _sp.IsOpen) _sp.Close();
    }

    void ReadLoop()
    {
        while (_run)
        {
            try {
                if (_sp == null || !_sp.IsOpen) break;
                string line = _sp.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line.ToUpper().Contains("XX")) continue;

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                lock (_lock) {
                    _lastValidTs = now;
                    if (!_triggered) {
                        _triggered = true;
                        _hasPulse = true;
                        File.WriteAllText(_presencePath, now.ToString());
                    }
                }
            } catch (Exception) { }
        }
    }

    void Update()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock) {
            if (_triggered && (now - _lastValidTs) >= (inactiveTimeout * 1000)) {
                _triggered = false;
                Debug.Log("[SERIAL] Sensor Rearmado.");
            }
        }
    }

    public bool ConsumePulse()
    {
        lock (_lock) {
            if (_hasPulse) { _hasPulse = false; return true; }
            return false;
        }
    }
}