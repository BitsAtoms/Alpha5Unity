using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialPortPresenceReader : MonoBehaviour
{
    [Header("Serial")]
    public string portName = "COM4";
    public int baudRate = 115200;
    public int readTimeoutMs = 200;

    [Header("Parsing")]
    [Tooltip("Si el sensor manda líneas tipo '1'/'0' o 'PRESENT'/'ABSENT', ajusta aquí.")]
    public bool pulseMode = true; // si true: cada '1' es un pulso (evento)
    
    public event Action<bool, long> OnPresence; // present, tsMs (o pulso)

    private SerialPort _sp;
    private Thread _thread;
    private volatile bool _run;

    void OnEnable()
    {
        StartReader();
    }

    void OnDisable()
    {
        StopReader();
    }

    void StartReader()
    {
        try
        {
            _sp = new SerialPort(portName, baudRate)
            {
                ReadTimeout = readTimeoutMs,
                NewLine = "\n"
            };
            _sp.Open();

            _run = true;
            _thread = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();

            Debug.Log($"[PRESENCE] Open {portName} @{baudRate}");
        }
        catch (Exception e)
        {
            Debug.LogError("[PRESENCE] Cannot open port: " + e.Message);
        }
    }

    void StopReader()
    {
        _run = false;
        try { _thread?.Join(300); } catch { }
        try { if (_sp != null && _sp.IsOpen) _sp.Close(); } catch { }
        _thread = null;
        _sp = null;
    }

    void ReadLoop()
    {
        while (_run)
        {
            try
            {
                string line = _sp.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                line = line.Trim();
                bool present = ParsePresence(line);

                long tsMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // En pulseMode, normalmente solo te interesa el "true"
                if (!pulseMode || present)
                {
                    // Enviar al main thread de forma segura:
                    UnityMainThreadDispatcher.Enqueue(() => OnPresence?.Invoke(present, tsMs));
                }
            }
            catch (TimeoutException) { }
            catch (Exception) { /* si se desconecta, aquí podrías reconectar */ }
        }
    }

    bool ParsePresence(string s)
    {
        // Ajusta según tu sensor:
        if (s == "1" || s.Equals("PRESENT", StringComparison.OrdinalIgnoreCase) || s.Equals("ON", StringComparison.OrdinalIgnoreCase))
            return true;
        if (s == "0" || s.Equals("ABSENT", StringComparison.OrdinalIgnoreCase) || s.Equals("OFF", StringComparison.OrdinalIgnoreCase))
            return false;

        // fallback: si no sabemos, considera cualquier cosa como "true"
        return true;
    }
}
