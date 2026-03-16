using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RemoteMenuReceiver : MonoBehaviour
{
    [Header("Identificador de Máquina")]
    [Tooltip("Debe coincidir EXACTAMENTE con el de la Tablet")]
    public string machineID = "PENALTYCHALLENGE";

    [Header("UDP")]
    public int listenPort = 7777;

    [Header("Escenas")]
    public string sceneDificil = "ProyectoFinalDificil";
    public string sceneStandard = "ProyectoFinalStandard";

    UdpClient udp;
    Thread thread;
    volatile bool running;

    string pendingCommand = null;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartListener();
        Debug.Log($"[REMOTE] Escuchando UDP en puerto {listenPort} para la máquina {machineID}");
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
                pendingCommand = msg;
            }
            catch
            {
            }
        }
    }

    void Update()
    {
        if (string.IsNullOrEmpty(pendingCommand)) return;

        string cmd = pendingCommand;
        pendingCommand = null;

        Debug.Log($"[REMOTE] Procesando '{cmd}'");

        // Convertimos a mayúsculas por seguridad (por si en el inspector escribes "Cabina1")
        string targetDificil = $"{machineID.ToUpperInvariant()}:DIFICIL";
        string targetStandard = $"{machineID.ToUpperInvariant()}:STANDARD";

        if (cmd == targetDificil)
        {
            Debug.Log("[REMOTE] Cargando escena dificil");
            SceneManager.LoadScene(sceneDificil);
        }
        else if (cmd == targetStandard)
        {
            Debug.Log("[REMOTE] Cargando escena standard");
            SceneManager.LoadScene(sceneStandard);
        }
    }
}