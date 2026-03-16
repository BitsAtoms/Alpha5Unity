using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SickPhotoelectricSensor : MonoBehaviour
{
    [Header("Conexión Microcontrolador (Arduino)")]
    public string portName = "COM8";
    public int baudRate = 115200;
    
    [Header("Anti-Rebote (Debounce)")]
    public float sensorCooldown = 1.0f; // Evita falsos dobles toques

    public event Action OnSensorTriggered;

    private SerialPort _sp;
    private Thread _thread;
    private volatile bool _run;
    private bool _pendingEvent = false;
    private double _lastTriggerTime = 0;
    private readonly object _lock = new object();

    void OnEnable() 
    {
        _run = true;
        _thread = new Thread(SerialLoop) { IsBackground = true };
        _thread.Start();
    }

    void SerialLoop() 
    {
        try 
        {
            _sp = new SerialPort(portName, baudRate) { ReadTimeout = 100, DtrEnable = true };
            _sp.Open();
            Debug.Log($"[SICK WSE12] Escuchando barrera infrarroja en {portName}");
            
            while (_run && _sp.IsOpen) 
            {
                try 
                {
                    string line = _sp.ReadLine();
                    if (line.Contains("HIT")) 
                    {
                        double now = GetUnixTimestamp();
                        lock (_lock) 
                        {
                            if (now - _lastTriggerTime >= sensorCooldown) 
                            {
                                _lastTriggerTime = now;
                                _pendingEvent = true;
                            }
                        }
                    }
                } catch (TimeoutException) { continue; }
            }
        } catch (Exception e) {
            Debug.LogError($"[SICK WSE12] Error de puerto: {e.Message}");
        }
    }

    void Update() 
    {
        if (_pendingEvent) 
        {
            _pendingEvent = false;
            OnSensorTriggered?.Invoke(); 
        }
    }

    private double GetUnixTimestamp() => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

    void OnDisable() 
    {
        _run = false;
        if (_sp != null && _sp.IsOpen) _sp.Close();
    }
}