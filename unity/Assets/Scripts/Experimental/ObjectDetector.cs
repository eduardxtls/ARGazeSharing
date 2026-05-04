using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;

public class ObjectDetector : MonoBehaviour
{
    [Header("References")]
    public UDPListener udpListener;
    public CueManager cueManager;

    [Header("AI")]
    public ModelAsset modelAsset;
    public GameObject highlightPrefab;

    private Worker worker;
    private List<GameObject> pool = new List<GameObject>();

    private void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);

        GameObject highlight = Instantiate(highlightPrefab);
        highlight.SetActive(false);
        pool.Add(highlight);
    }

    private void Update()
    {
        string currentLabel = udpListener.GetLatestLabel();

        if (!string.IsNullOrEmpty(currentLabel))
        {
            ProcessDetection(currentLabel);
        }

    }

    void ProcessDetection(string label)
    {
        if (label == "None")
        {
            foreach (var g in pool) g.SetActive(false);
        }
        UpdateVisuals(label);
    }

    void UpdateVisuals(string label)
    {
        GameObject highlight = pool[0];
        highlight.SetActive(true);

        GameObject targetObject = GameObject.Find(label);

        if (targetObject != null)
        {
            Vector3 targetPosition = targetObject.transform.position;

            highlight.transform.position = targetPosition;

            if (cueManager != null)
            {
                cueManager.UpdateCueTransform(targetPosition, Quaternion.identity, Vector3.one);
            }
        }
    }

    private void OnDestroy() => worker?.Dispose();
  
}