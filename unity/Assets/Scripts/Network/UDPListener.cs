using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class UDPListener : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 8080;

    [Header("References")]
    public CueManager cueManager;

    private UdpClient client;
    private Thread receiveThread;
    private bool running = false;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    // private string latestLabel = string.Empty;
    private string latestLabel = "keyboard";

    // Handlers for spatial and design change messages
    private Dictionary<string, Action<string>> messageHandlers;

    void Start()
    {
        messageHandlers = new Dictionary<string, Action<string>>()
        {
            {"design" , json => 
                HandleDesign(JsonUtility.FromJson<DesignMessage>(json)) },

            {"detection" , json =>
                HandleDetection(JsonUtility.FromJson<DetectionMessage>(json)) }
        };

        try
        {
            client = new UdpClient(port);
            running = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"UDP Listener started on port {port}");
        }
        catch (Exception error)
        {
            Debug.LogError($"Failed to start UDP Listener: {error.Message}");
        }
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);

        while (running)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string json = Encoding.UTF8.GetString(data);
                messageQueue.Enqueue(json);
            }
            catch (Exception error)
            {
                if (running) Debug.LogError($"UDP Listener error: {error.Message}");
            }
        }
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string json))
        {
            HandleMessage(json);
        }
    }

    void HandleMessage(string json)
    {
        BaseMessage baseMessage = JsonUtility.FromJson<BaseMessage>(json);

        if (baseMessage == null || string.IsNullOrEmpty(baseMessage.type))
        {
            Debug.Log("Inavlid Message Received");
            return;
        }

        // Handle the message based on its type
        if (messageHandlers.TryGetValue(baseMessage.type, out var handler))
        {
            handler(json);
        }
        else
        {
            Debug.Log($"Unknown message type: {baseMessage.type}");
        }
    }

    void HandleDesign(DesignMessage message)
    {
        if (message != null && cueManager != null)
        {
            cueManager.ApplyDesign(message);
        }
    }

    void HandleDetection(DetectionMessage message)
    {
        if (message != null && !string.IsNullOrEmpty(message.label))
        {
            latestLabel = message.label;
            Debug.Log($"Detection - Label: {message.label}");
        }
    }

    public string GetLatestLabel()
    {
        return latestLabel;
    }

    private void OnApplicationQuit()
    {
        running = false;
        client?.Close();
    }
}