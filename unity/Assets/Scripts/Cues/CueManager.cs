using UnityEngine;

[ExecuteAlways]
public class CueManager : MonoBehaviour
{
    // ==========================================
    // EDITOR INCOMING MESSAGE SIMULATORS
    // Right-click the CueManager component in the Inspector to try these!
    // ==========================================

    [ContextMenu("Simulate Message: Size -> Huge")]
    private void TestMessageSizeHuge()
    {
        Debug.Log("--- Simulating Network Message: SetSize('huge') ---");
        SetSize("huge");
        // Force the layout engine to refresh the changes visually
        UpdateCueTransform(transform.position, transform.rotation, simulatedScale);
    }

    [ContextMenu("Simulate Message: Size -> Small")]
    private void TestMessageSizeSmall()
    {
        Debug.Log("--- Simulating Network Message: SetSize('small') ---");
        SetSize("small");
        UpdateCueTransform(transform.position, transform.rotation, simulatedScale);
    }

    [ContextMenu("Simulate Message: Color -> Bright Cyan")]
    private void TestMessageColorCyan()
    {
        Debug.Log("--- Simulating Network Message: SetColor('#00FFFF') ---");
        SetColor("#00FFFF");
    }

    [ContextMenu("Simulate Message: Alpha -> 30%")]
    private void TestMessageAlphaLow()
    {
        Debug.Log("--- Simulating Network Message: SetAlpha('0.3') ---");
        SetAlpha("0.3");
    }

    [ContextMenu("Simulate Message: Alpha -> 100%")]
    private void TestMessageAlphaFull()
    {
        Debug.Log("--- Simulating Network Message: SetAlpha('1.0') ---");
        SetAlpha("1.0");
    }

    [ContextMenu("Simulate Message: Design -> Overlay")]
    private void TestMessageDesignOverlay()
    {
        Debug.Log("--- Simulating Network Message: SetDesign('overlay') ---");
        SetDesign("overlay");
    }

    [ContextMenu("Simulate Message: Design -> Outline")]
    private void TestMessageDesignOutline()
    {
        Debug.Log("--- Simulating Network Message: SetDesign('outline') ---");
        SetDesign("outline");
    }

    [Header("Editor Simulation")]
    public bool simulateInEditor = false;
    public Vector3 simulatedScale = new Vector3(1f, 1f, 1f);
    [Range(0.01f, 1.0f)]
    public float simulatedThickness = 0.05f; // Set directly in world meters (e.g., 0.05 = 5cm)

    [Header("Smoothness Settings")]
    public float outlineSmooth = 0.05f;
    public float overlaySmooth = 0.1f;

    [Header("Default Design")]
    public string currentDesign = "outline";

    [Header("Cue GameObjects")]
    public GameObject overlayCue;
    public GameObject outlineCue;
    public Transform innerTransform;
    public Transform outerTransform;

    private Renderer overlayRenderer;
    private SpriteRenderer outerSpriteRenderer;

    private Vector3 outlineVelocity = Vector3.zero;
    private Vector3 overlayVelocity = Vector3.zero;

    // Outline size settings
    private float currentThickness = 0.1f;
    private Vector2 currentSize = new Vector2(1f, 1f);

    private void OnValidate()
    {
        CacheMaskingReferences();
        ExecuteLayoutUpdates();
    }

    private void Update()
    {
        // Force the editor viewport to redraw continuously while dragging sliders
        if (!Application.isPlaying && simulateInEditor)
        {
            ExecuteLayoutUpdates();
        }
    }

    private void Awake()
    {
        if (overlayCue) overlayRenderer = overlayCue.GetComponent<Renderer>();

        // Get the references for the outline cue
        CacheMaskingReferences();

        // Default initialization
        SetDesign(currentDesign);
        SetColor("#32cd32");
        SetAlpha("0.25");
        SetVisibility(true);
    }

    public void CacheMaskingReferences()
    {
        if (outerTransform != null && outerSpriteRenderer == null)
        {
            outerSpriteRenderer = outerTransform.GetComponent<SpriteRenderer>();
        }

        if (overlayCue != null && overlayRenderer == null)
        {
            overlayRenderer = overlayCue.GetComponent<Renderer>();
        }
    }

    private void ExecuteLayoutUpdates()
    {
        if (simulateInEditor)
        {
            // currentThickness = simulatedThickness;
            UpdateCueTransform(transform.position, transform.rotation, simulatedScale);

#if UNITY_EDITOR
            // Instantly repaints the scene view to bypass Unity's editor rendering freeze
            UnityEditor.SceneView.RepaintAll();
#endif
        }
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
        currentSize = new Vector2(scale.x, scale.y);

        if (outlineCue)
        {
            if (innerTransform != null)
            {
                innerTransform.position = Application.isPlaying ? Vector3.SmoothDamp(innerTransform.position, position, ref outlineVelocity, outlineSmooth) : position;
                innerTransform.rotation = rotation;
                innerTransform.localScale = new Vector3(currentSize.x, currentSize.y, 1f);
            }

            if (outerTransform != null)
            {
                outerTransform.position = Application.isPlaying ? innerTransform.position : position; // Sync position lock
                outerTransform.rotation = rotation;

                // Total visual size = core tracked dimensions + thickness on both sides
                float totalWidth = currentSize.x + (currentThickness * 2f);
                float totalHeight = currentSize.y + (currentThickness * 2f);
                outerTransform.localScale = new Vector3(totalWidth, totalHeight, 1f);
            }
        }

        if (overlayCue)
        {
            overlayCue.transform.position = Vector3.SmoothDamp(overlayCue.transform.position, position, ref overlayVelocity, overlaySmooth);
            overlayCue.transform.rotation = rotation;
            overlayCue.transform.localScale = scale;
        }
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
        switch (size.ToLower())
        {
            case "tiny":    currentThickness = 0.01f; break;
            case "small":   currentThickness = 0.025f; break;
            case "medium":  currentThickness = 0.5f; break;
            case "large":   currentThickness = 0.1f; break;
            case "huge":    currentThickness = 0.15f; break;
            default:        Debug.Log($"Unknown size value: {size}"); return;
        }

        Debug.Log($"Design Change - Size: {size}");
    }

    public void SetColor(string color)
    {
        if (UnityEngine.ColorUtility.TryParseHtmlString(color, out Color newColor))
        {
            newColor.a = outerSpriteRenderer.color.a; // Preserve current alpha - same for both cues
            outerSpriteRenderer.color = newColor;
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
            if (outerSpriteRenderer != null)
            {
                Color color = outerSpriteRenderer.material.color;
                color.a = alphaValue;
                outerSpriteRenderer.material.color = color;
            }

            if (overlayRenderer != null)
            {
                Color color = overlayRenderer.material.color;
                color.a = alphaValue;
                overlayRenderer.material.color = color;
            }
            
            Debug.Log($"Design Change - Alpha: {alpha}");
        }
        else
        {
            Debug.Log($"Invalid alpha value: {alpha}");
        }
    }
}