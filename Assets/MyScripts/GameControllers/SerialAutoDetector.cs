using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialAutoDetector : MonoBehaviour
{
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
        _thread = new Thread(AutoDetectLoop) { IsBackground = true };
        _thread.Start();
    }

    void AutoDetectLoop() {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0) {
            Debug.LogWarning("[SERIAL] No hay puertos COM disponibles.");
            return;
        }

        foreach (string port in ports) {
            try {
                // Configuración más robusta
                _sp = new SerialPort(port, baudRate) { 
                    ReadTimeout = 500, 
                    DtrEnable = true, // Necesario para algunos sensores/Arduinos
                    RtsEnable = true 
                };
                _sp.Open();
                Debug.Log($"[SERIAL] Puerto {port} ABIERTO. Esperando datos...");
                
                _run = true;
                while (_run && _sp != null && _sp.IsOpen) {
                    try {
                        string line = _sp.ReadLine(); // Intenta leer línea
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        // Debug opcional para ver qué llega exactamente
                        // UnityMainThreadDispatcher.Enqueue(() => Debug.Log($"[SERIAL RAW] {line}"));

                        if (line.ToUpper().Contains("XX")) continue;

                        lock (_lock) {
                            _lastValidTimestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                            if (!_triggered) { 
                                _triggered = true; 
                                _pendingEvent = true; 
                            }
                        }
                    } catch (TimeoutException) {
                        // Es normal si el sensor no envía nada constantemente
                        continue; 
                    }
                }
            } catch (Exception e) {
                Debug.LogWarning($"[SERIAL] Error en {port}: {e.Message}");
                if (_sp != null) _sp.Close();
            }
        }
    }

    void Update() {
        if (_pendingEvent) {
            _pendingEvent = false;
            OnSensorTriggered?.Invoke();
            Debug.Log("✅ [EVENTO] ¡Sensor detectado con éxito!");
        }

        // Lógica de rearmado
        double now = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        lock (_lock) {
            if (_triggered && (now - _lastValidTimestamp) >= inactiveTimeout) {
                _triggered = false;
                // Debug.Log("[SERIAL] Rearmado listo");
            }
        }
    }

    void OnDisable() {
        _run = false;
        if (_sp != null && _sp.IsOpen) {
            _sp.Close();
            _sp = null;
        }
    }
}