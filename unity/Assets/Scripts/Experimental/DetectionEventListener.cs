using UnityEngine;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks.AIBlocks;

public class DetectionEventListener : MonoBehaviour
{
    [SerializeField] private UDPListener udpListener;

    public ObjectDetectionAgent agent;
    public GameObject boxPrefab;

    private List<GameObject> _activeBoxes = new List<GameObject>();
    private Queue<GameObject> _pool = new Queue<GameObject>();

    private void OnEnable()
    {
        if (agent != null) agent.OnDetectionResponseReceived.AddListener(RefreshVisuals);
    }

    private void OnDisable()
    {
        if (agent != null) agent.OnDetectionResponseReceived.RemoveListener(RefreshVisuals);
    }

    private void RefreshVisuals(List<BoxData> batch)
    {
        foreach (var box in _activeBoxes)
        {
            box.SetActive(false);
            _pool.Enqueue(box);
        }
        _activeBoxes.Clear();

        string target = "laptop"; // For testing
        // string target = udpListener.GetLatestMessage().Trim().ToLower();
        if (string.IsNullOrEmpty(target)) return;

        foreach (var data in batch)
        {
            // Only visualize if the label matches UDP command
            if (data.label.ToLower().Contains(target))
            {
                GameObject visual = GetOrCreateBox();

                // Use data from the Agent to position the box
                visual.transform.SetPositionAndRotation(data.position, data.rotation);
                visual.transform.localScale = data.scale;

                visual.SetActive(true);
                _activeBoxes.Add(visual);
            }
        }
    }

    private GameObject GetOrCreateBox()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        return Instantiate(boxPrefab);
    }
}