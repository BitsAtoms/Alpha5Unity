using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SickLidarDetector : MonoBehaviour
{
    [Header("SICK Ethernet Config")]
    public string sickIP = "192.168.0.1"; // IP por defecto del SICK
    public int sickPort = 2112; // Puerto de datos CoLa

    public event Action<float, float, double> OnGoalDetected;

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private Thread _lidarThread;
    private volatile bool _isRunning = false;
    private volatile bool _isDetectionArmed = false;

    void OnEnable()
    {
        _isRunning = true;
        _lidarThread = new Thread(LidarTcpLoop) { IsBackground = true };
        _lidarThread.Start();
    }

    void Update() 
    {
        // Solo escaneamos cuando el juego espera un tiro
        if (GameManager.I != null) 
        {
            _isDetectionArmed = GameManager.I.CanShoot();
        }
    }

    void LidarTcpLoop()
    {
        try
        {
            _tcpClient = new TcpClient(sickIP, sickPort);
            _stream = _tcpClient.GetStream();
            byte[] buffer = new byte[8192];

            // Pedir flujo continuo de datos al SICK
            byte[] startCmd = Encoding.ASCII.GetBytes("\x02sEN LMDscandata 1\x03");
            _stream.Write(startCmd, 0, startCmd.Length);
            
            Debug.Log($"[SICK LiDAR] Conectado a {sickIP}:{sickPort}");

            while (_isRunning && _tcpClient.Connected)
            {
                if (_stream.DataAvailable)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    
                    if (_isDetectionArmed) 
                    {
                        string rawData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        ProcessSickTelegram(rawData);
                    }
                }
                else
                {
                    Thread.Sleep(5); // Descanso CPU
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SICK LiDAR] Error TCP: " + e.Message);
        }
    }

    void ProcessSickTelegram(string data)
    {
        // TODO: Aquí leeremos los grados y distancias del telegrama HEX del SICK.
        // Cuando detectemos la pelota, llamaremos a:
        // double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        // UnityMainThreadDispatcher.Enqueue(() => { OnGoalDetected?.Invoke(lat01, h01, ts); });
    }

    void OnDisable()
    {
        _isRunning = false;
        if (_stream != null) _stream.Close();
        if (_tcpClient != null) _tcpClient.Close();
    }
}