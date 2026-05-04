using UnityEngine;
using Unity.InferenceEngine;

public class InferenceManager : MonoBehaviour
{
    public ModelAsset modelAsset;
    private Model model;
    public Worker worker;

    private void Start()
    {
        model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);
    }

    public float[] GetPrediciton(Texture2D cameraTexture)
    {
        using Tensor<float> input = TextureConverter.ToTensor(cameraTexture);

        worker.Schedule(input);

        using Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        return output.DownloadToArray();
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}