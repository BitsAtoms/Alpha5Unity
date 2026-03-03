using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialSensorEvent : MonoBehaviour
{
    [Header("Configuración Puerto Fijo")]
    public string portName = "COM8";
    public int baudRate = 115200;
    public float inactiveTimeout = 0.6f;

    public event Action OnSensorTriggered;

    private SerialPort _sp;
    private Thread _thread;
    private volatile bool _run;
    private bool _pendingEvent = false;
    private double _lastValidTimestamp = 0;
    private bool _triggered = false;
    private readonly object _lock = new object();

    void OnEnable() {
        _run = true;
        _thread = new Thread(FixedPortLoop) { IsBackground = true };
        _thread.Start();
    }

    void FixedPortLoop() {
        try {
            _sp = new SerialPort(portName, baudRate) { ReadTimeout = 500, DtrEnable = true, RtsEnable = true };
            _sp.Open();
            Debug.Log($"[SENSOR PORTERO] Conectado en {portName}");
            
            while (_run && _sp != null && _sp.IsOpen) {
                try {
                    string line = _sp.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.ToUpper().Contains("XX")) continue;

                    lock (_lock) {
                        _lastValidTimestamp = GetUnixTimestamp();
                        if (!_triggered) { 
                            _triggered = true; 
                            _pendingEvent = true; 
                        }
                    }
                } catch (TimeoutException) { continue; }
                catch (System.IO.IOException) { break; } 
            }
        } catch (Exception e) {
            Debug.LogError($"[SENSOR PORTERO] Error en {portName}: {e.Message}");
        }
    }

    void Update() {
        if (_pendingEvent) {
            _pendingEvent = false;
            OnSensorTriggered?.Invoke(); 
        }

        double now = GetUnixTimestamp();
        lock (_lock) {
            if (_triggered && (now - _lastValidTimestamp) >= inactiveTimeout) _triggered = false;
        }
    }

    private double GetUnixTimestamp() => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

    void OnDisable() {
        _run = false;
        if (_sp != null && _sp.IsOpen) {
            _sp.Close();
            _sp = null;
        }
    }
}