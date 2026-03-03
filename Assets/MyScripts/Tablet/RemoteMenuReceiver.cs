using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RemoteMenuReceiver : MonoBehaviour
{
    [Header("UDP")]
    public int listenPort = 7777;

    [Header("Escenas")]
    public string sceneAdulto = "ProyectoFinalAdulto";
    public string sceneNino = "ProyectoFinalNiño";

    UdpClient udp;
    Thread thread;
    volatile bool running;

    // Esto guarda el último comando recibido (lo procesa en Update para estar en el hilo principal)
    string pendingCommand = null;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartListener();
        Debug.Log($"[REMOTE] Escuchando UDP en puerto {listenPort}");
    }

    void OnDestroy() => StopListener();
    void OnApplicationQuit() => StopListener();

    void StartListener()
    {
        running = true;
        udp = new UdpClient(listenPort);
        thread = new Thread(ListenLoop) { IsBackground = true };
        thread.Start();
    }

    void StopListener()
    {
        running = false;
        try { udp?.Close(); } catch { }
        try { thread?.Join(200); } catch { }
        udp = null;
        thread = null;
    }

    void ListenLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);

        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref ep);
                string msg = Encoding.UTF8.GetString(data).Trim().ToUpperInvariant();
                Debug.Log($"[REMOTE] Recibido '{msg}' desde {ep.Address}");
                pendingCommand = msg; // se procesa en Update
            }
            catch { /* al cerrar el socket puede lanzar excepción: ignoramos */ }
        }
    }

    void Update()
    {
        if (string.IsNullOrEmpty(pendingCommand)) return;

        string cmd = pendingCommand;
        pendingCommand = null;

        EnsureSettings();

        if (cmd == "ADULTO" || cmd == "NORMAL")
        {
            GameSettings.I.SetMode(GameMode.Normal);
            SceneManager.LoadScene(sceneAdulto);
        }
        else if (cmd == "NINO" || cmd == "NIÑO")
        {
            GameSettings.I.SetMode(GameMode.Nino);
            SceneManager.LoadScene(sceneNino);
        }
    }

    void EnsureSettings()
    {
        if (GameSettings.I != null) return;
        var go = new GameObject("GameSettings");
        go.AddComponent<GameSettings>();
    }
}
