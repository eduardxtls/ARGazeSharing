using Unity.VisualScripting;
using UnityEngine;

public class CueManager : MonoBehaviour
{
    [Header("Settings")]
    public float outlineSmooth = 0.05f;
    public float overlaySmooth = 0.1f;

    public GameObject overlayCue;
    public GameObject outlineCue;

    private Renderer overlayRenderer;
    private Renderer outlineRenderer;

    private string currentDesign = "none";
    private Vector3 outlineVelocity = Vector3.zero;
    private Vector3 overlayVelocity = Vector3.zero;

    private void Awake()
    {
        if (overlayCue) overlayRenderer = overlayCue.GetComponent<Renderer>();
        if (outlineCue) outlineRenderer = outlineCue.GetComponent<Renderer>();

        SetDesign("outline");
        SetVisibility(true);
    }

    public void ApplyDesign(DesignMessage design)
    {
        string value = design.value;
        switch (design.parameter.ToLower())
        {
            case "cue_type":    SetDesign(value);   break;
            case "size":        SetSize(value);     break;
            case "color":       SetColor(value);    break;
            case "alpha":       SetAlpha(value);    break;
        }
    }

    public void UpdateCueTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        outlineCue.transform.position = Vector3.SmoothDamp(
            outlineCue.transform.position,
            position,
            ref outlineVelocity,
            outlineSmooth
        );
        outlineCue.transform.rotation = rotation;
        outlineCue.transform.localScale = scale;

        overlayCue.transform.position = Vector3.SmoothDamp(
            overlayCue.transform.position,
            position,
            ref overlayVelocity,
            overlaySmooth
        );
        overlayCue.transform.rotation = rotation;
        overlayCue.transform.localScale = scale;
    }

    public void SetVisibility(bool visible)
    {
        // Only currently enabled cue is shown
        outlineCue.SetActive(visible && currentDesign == "outline");
        overlayCue.SetActive(visible && currentDesign == "overlay");
    }

    // Setters for design parameters
    public void SetDesign(string design)
    {
        currentDesign = design.ToLower();
        
        outlineCue.SetActive(currentDesign == "outline");
        overlayCue.SetActive(currentDesign == "overlay");

        Debug.Log($"Design Change - Design: {design}");
    }
    public void SetSize(string size)
    {
        float scale = 1.0f;
        switch (size.ToLower())
        {
            case "tiny":    scale = 0.25f; break;
            case "small":   scale = 0.5f; break;
            case "medium":  scale = 1.0f; break;
            case "large":   scale = 1.5f; break;
            case "huge":    scale = 2.0f; break;
            default:        Debug.Log($"Unknown size value: {size}"); return;
        }
        outlineCue.transform.localScale = Vector3.one * scale;
        overlayCue.transform.localScale = Vector3.one * scale;
        Debug.Log($"Design Change - Size: {size}");
    }

    public void SetColor(string color)
    {
        if (UnityEngine.ColorUtility.TryParseHtmlString(color, out Color newColor))
        {
            newColor.a = outlineRenderer.material.color.a; // Preserve current alpha - same for both cues
            outlineRenderer.material.color = newColor;
            overlayRenderer.material.color = newColor;
            Debug.Log("Design Change - Color: " + color);
        }
        else
        {
            Debug.Log($"Invalid color value: {color}");
        }   
    }

    public void SetAlpha(string alpha)
    {
        if (float.TryParse(alpha, out float alphaValue))
        {
            Color outlineColor = outlineCue.GetComponent<Renderer>().material.color;
            Color overlayColor = overlayCue.GetComponent<Renderer>().material.color;
            
            outlineColor.a = alphaValue;
            overlayColor.a = alphaValue;
            
            outlineCue.GetComponent<Renderer>().material.color = outlineColor;
            overlayCue.GetComponent<Renderer>().material.color = overlayColor;
            
            Debug.Log($"Design Change - Alpha: {alpha}");
        }
        else
        {
            Debug.Log($"Invalid alpha value: {alpha}");
        }
    }
}
