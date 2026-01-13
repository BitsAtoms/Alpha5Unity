using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class LidarReceiver : MonoBehaviour
{
    UdpClient udpClient;
    public int puerto = 5005;

    void Start()
    {
        udpClient = new UdpClient(puerto);
        udpClient.BeginReceive(ReceiveData, null);
    }

    void ReceiveData(System.IAsyncResult result)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, puerto);
        byte[] data = udpClient.EndReceive(result, ref ip);
        string mensaje = Encoding.UTF8.GetString(data).Trim();

        Debug.Log("LiDAR dice: " + mensaje);

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance es NULL");
            udpClient.BeginReceive(ReceiveData, null);
            return;
        }

        if (mensaje == "GOL")
        {
            GameManager.Instance.GoalScored();
        }
        else if (mensaje == "FALLO")
        {
            GameManager.Instance.ShotFail();
        }

        udpClient.BeginReceive(ReceiveData, null);
    }

    void OnDestroy()
    {
        if (udpClient != null)
            udpClient.Close();
    }
}
