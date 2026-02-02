using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialPresenceReader : MonoBehaviour
{
    [Header("Serial")]
    public string portName = "COM4";
    public int baudRate = 115200;

    [Header("Output (optional file like the LIDAR does)")]
    public bool writePresenceFile = true;
    public string presenceFileName = "presence_result.txt";

    public event Action<bool, long> OnPresence; // present, tsMs

    SerialPort _sp;
    Thread _thread;
    volatile bool _run;

    string _presencePath;
    long _lastTs = -1;

    void Awake()
    {
        _presencePath = Path.Combine(Application.dataPath, "../Config", presenceFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(_presencePath));
    }

    void OnEnable() => StartPort();
    void OnDisable() => StopPort();
    void OnApplicationQuit() => StopPort();

    void StartPort()
    {
        try
        {
            _sp = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 200
            };
            _sp.Open();

            _run = true;
            _thread = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();

            Debug.Log($"[PRESENCE] Open {portName} @{baudRate}");
        }
        catch (Exception e)
        {
            Debug.LogError("[PRESENCE] Cannot open: " + e.Message);
        }
    }

    void StopPort()
    {
        _run = false;
        try { _thread?.Join(300); } catch {}
        try { if (_sp != null && _sp.IsOpen) _sp.Close(); } catch {}
        _thread = null;
        _sp = null;
    }

    void ReadLoop()
    {
        while (_run)
        {
            try
            {
                var line = _sp.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                bool present = Parse(line.Trim());
                long tsMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (tsMs == _lastTs) continue;
                _lastTs = tsMs;

                UnityMainThreadQueue.Enqueue(() =>
                {
                    Debug.Log($"[PRESENCE] present={present} tsMs={tsMs}");
                    OnPresence?.Invoke(present, tsMs);

                    if (writePresenceFile)
                        File.WriteAllText(_presencePath, $"{(present ? 1 : 0)},{tsMs}\n");
                });
            }
            catch (TimeoutException) {}
            catch (Exception) {}
        }
    }

    bool Parse(string s)
    {
        if (s == "1" || s.Equals("ON", StringComparison.OrdinalIgnoreCase) || s.Equals("PRESENT", StringComparison.OrdinalIgnoreCase))
            return true;
        if (s == "0" || s.Equals("OFF", StringComparison.OrdinalIgnoreCase) || s.Equals("ABSENT", StringComparison.OrdinalIgnoreCase))
            return false;

        // fallback (si manda algo raro)
        return true;
    }
}
