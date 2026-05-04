[System.Serializable]
public class DetectionMessage
{
    public string type;
    public string label;
    public string color;
    public float[] position;
    public float[] marker_tvec;
    public float[] marker_rvec;
    public float[] gaze_2d;
}