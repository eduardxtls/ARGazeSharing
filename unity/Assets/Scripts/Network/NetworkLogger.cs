using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class NetworkLogger : MonoBehaviour
{
    public static NetworkLogger Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private string laptopIP = "";
    [SerializeField] private int laptopPort = 5005;

    public string LaptopIP
    {
        get => laptopIP;
        set => laptopIP = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateLaptopIP(string ip)
    {
        if (!string.IsNullOrEmpty(laptopIP) && laptopIP != ip)
        {
            laptopIP = ip;
        }
    }

    public void SendLog(string csvLine)
    {
        if (string.IsNullOrEmpty(laptopIP))
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(laptopIP, laptopPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(csvLine);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TCP Send Failed: {e.Message}");
            }
        });
    }
}
